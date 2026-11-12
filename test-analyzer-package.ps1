param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [ValidateSet('NTComponents', 'NTComponents.Analyzers')][string]$ExpectedPackageId = 'NTComponents.Analyzers'
)

$ErrorActionPreference = 'Stop'

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $requiredEntries = @(
        'analyzers/dotnet/cs/NTComponents.Analyzers.dll',
        'analyzers/dotnet/cs/NTComponents.Analyzers.CodeFixes.dll',
        'README.md',
        'Logo.png'
    )

    foreach ($requiredEntry in $requiredEntries) {
        if (-not ($archive.Entries | Where-Object FullName -eq $requiredEntry)) {
            throw "Analyzer package is missing required entry '$requiredEntry'."
        }
    }

    if ($ExpectedPackageId -eq 'NTComponents.Analyzers' -and ($archive.Entries | Where-Object { $_.FullName -like 'lib/*.dll' -or $_.FullName -like 'lib/*/*.dll' })) {
        throw 'Analyzer package must not expose its assembly as a compile or runtime library.'
    }

    $nuspecEntry = $archive.Entries | Where-Object FullName -like '*.nuspec' | Select-Object -First 1
    if (-not $nuspecEntry) {
        throw 'Analyzer package does not contain a nuspec file.'
    }

    $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try {
        [xml]$nuspec = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }

    $packageId = [string]$nuspec.package.metadata.id
    $packageVersion = [string]$nuspec.package.metadata.version
    if ($packageId -ne $ExpectedPackageId -or [string]::IsNullOrWhiteSpace($packageVersion)) {
        throw "Unexpected analyzer package identity '$packageId' version '$packageVersion'."
    }
}
finally {
    $archive.Dispose()
}

$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "ntcomponents-analyzer-package-$([Guid]::NewGuid().ToString('N'))"
$feedDirectory = Join-Path $temporaryDirectory 'feed'
$projectDirectory = Join-Path $temporaryDirectory 'consumer'
$packagesDirectory = Join-Path $temporaryDirectory 'packages'

try {
    New-Item -ItemType Directory -Path $feedDirectory, $projectDirectory | Out-Null
    Copy-Item -LiteralPath $resolvedPackagePath -Destination $feedDirectory

    $escapedFeedDirectory = [System.Security.SecurityElement]::Escape($feedDirectory)
    $escapedPackagesDirectory = [System.Security.SecurityElement]::Escape($packagesDirectory)
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RestoreSources>$escapedFeedDirectory;https://api.nuget.org/v3/index.json</RestoreSources>
    <RestorePackagesPath>$escapedPackagesDirectory</RestorePackagesPath>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$packageId" Version="$packageVersion" />
  </ItemGroup>
</Project>
"@

    $source = if ($packageId -eq 'NTComponents') {
        @'
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents;

public static class ButtonFactory {
    public static void Build(RenderTreeBuilder builder) {
        builder.OpenComponent<NTButton>(0);
        builder.CloseComponent();
    }
}
'@
    }
    else {
        @'
namespace Microsoft.AspNetCore.Components.Rendering {
    public sealed class RenderTreeBuilder {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddAttribute(int sequence, string name, object? value) { }
        public void CloseComponent() { }
    }
}

namespace NTComponents {
    public sealed class NTButton { }
    public enum NTButtonVariant { Elevated, Filled, Tonal, Outlined, Text }
    public enum TnTColor { None, Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, InverseSurface }
    public enum NTColor { Transparent, Primary, OnPrimary, SecondaryContainer, OnSecondaryContainer, SurfaceContainerLow, InverseSurface }
    public enum NTElevation { None, Lowest, Low, Medium, High, Highest }
}

public static class ButtonFactory {
    public static object LegacyColor => NTComponents.TnTColor.Primary;
    public static void Build(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
        builder.OpenComponent<NTComponents.NTButton>(0);
        builder.CloseComponent();
    }
}
'@
    }

    $projectPath = Join-Path $projectDirectory 'AnalyzerPackageConsumer.csproj'
    [System.IO.File]::WriteAllText($projectPath, $project, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText((Join-Path $projectDirectory 'ButtonFactory.cs'), $source, [System.Text.UTF8Encoding]::new($false))

    $restoreOutput = & dotnet restore $projectPath --ignore-failed-sources --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Analyzer package consumer restore failed:`n$($restoreOutput -join [Environment]::NewLine)"
    }

    $buildOutput = & dotnet build $projectPath --no-restore --configuration Release --verbosity minimal 2>&1
    $buildText = $buildOutput -join [Environment]::NewLine
    if ($LASTEXITCODE -ne 0) {
        throw "Analyzer package consumer build failed:`n$buildText"
    }
    if ($buildText -notmatch '\bNTC1008\b') {
        throw "Analyzer package consumer build did not report NTC1008:`n$buildText"
    }
    # TnTColor no longer ships in NTComponents, so only the stub consumer of the standalone analyzer package can trigger NTC1077.
    $expectsMigration = $packageId -eq 'NTComponents.Analyzers'
    if ($expectsMigration -and $buildText -notmatch '\bNTC1077\b') {
        throw "Analyzer package consumer build did not report NTC1077:`n$buildText"
    }

    Write-Host "Verified $packageId $packageVersion, packaged code fixes, and observed $(if ($expectsMigration) { 'NTC1008 and NTC1077' } else { 'NTC1008' }) in a consumer build."
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        $resolvedTemporaryDirectory = (Resolve-Path -LiteralPath $temporaryDirectory).Path
        $expectedTemporaryDirectory = [System.IO.Path]::GetFullPath($temporaryDirectory)
        $temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
        if ($resolvedTemporaryDirectory -ne $expectedTemporaryDirectory -or -not $resolvedTemporaryDirectory.StartsWith($temporaryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an unexpected temporary directory '$resolvedTemporaryDirectory'."
        }
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
