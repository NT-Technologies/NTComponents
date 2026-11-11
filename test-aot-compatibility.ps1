param([string[]]$frameworks, [string[]]$runtimes, [switch]$SkipBrowserSmoke)

$ErrorActionPreference = 'Stop'

$rootDirectory = $PSScriptRoot
$projectPath = Join-Path $rootDirectory 'Tests/AotCompatibility.TestApp/AotCompatibility.TestApp.csproj'

if (-not $frameworks -or $frameworks.Length -eq 0) {
    if ($env:TARGET_FRAMEWORK) {
        $frameworks = $env:TARGET_FRAMEWORK -split '[,;]' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    }
}

$targetFrameworks = @('net10.0', 'net11.0')
if ($frameworks -and $frameworks.Length -gt 0) {
    $targetFrameworks = $frameworks
}

if ($runtimes -and $runtimes.Length -gt 0) {
    Write-Host "Runtime identifiers are ignored for Blazor WebAssembly AOT validation: $($runtimes -join ', ')"
}

function Get-FreePort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Parse('127.0.0.1'), 0)
    $listener.Start()
    try {
        return $listener.LocalEndpoint.Port
    }
    finally {
        $listener.Stop()
    }
}

function Get-BrowserExecutable {
    $candidates = @()
    $configuredBrowser = [Environment]::GetEnvironmentVariable('AOT_SMOKE_BROWSER')
    if ($configuredBrowser) {
        $candidates += $configuredBrowser
    }

    if ($IsWindows -or [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $programFiles = [Environment]::GetEnvironmentVariable('ProgramFiles')
        $programFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
        $localAppData = [Environment]::GetEnvironmentVariable('LOCALAPPDATA')

        $candidates += @(
            (Join-Path $programFiles 'Google/Chrome/Application/chrome.exe'),
            (Join-Path $programFilesX86 'Google/Chrome/Application/chrome.exe'),
            (Join-Path $localAppData 'Google/Chrome/Application/chrome.exe'),
            (Join-Path $programFiles 'Microsoft/Edge/Application/msedge.exe'),
            (Join-Path $programFilesX86 'Microsoft/Edge/Application/msedge.exe'),
            (Join-Path $localAppData 'Microsoft/Edge/Application/msedge.exe')
        ) | Where-Object { $_ -and $_ -ne '' }
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
        $candidates += @(
            '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
            '/Applications/Chromium.app/Contents/MacOS/Chromium',
            '/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge'
        )
    }

    $playwrightBrowsersPath = [Environment]::GetEnvironmentVariable('PLAYWRIGHT_BROWSERS_PATH')
    $userProfile = [Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)
    $playwrightCacheRoots = @()
    if ($playwrightBrowsersPath -and $playwrightBrowsersPath -ne '0') {
        $playwrightCacheRoots += $playwrightBrowsersPath
    }

    if ($IsWindows -or [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $localAppData = [Environment]::GetEnvironmentVariable('LOCALAPPDATA')
        if ($localAppData) {
            $playwrightCacheRoots += Join-Path $localAppData 'ms-playwright'
        }
    }
    elseif ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
        if ($userProfile) {
            $playwrightCacheRoots += Join-Path $userProfile 'Library/Caches/ms-playwright'
        }
    }
    else {
        $cacheHome = [Environment]::GetEnvironmentVariable('XDG_CACHE_HOME')
        if (-not $cacheHome -and $userProfile) {
            $cacheHome = Join-Path $userProfile '.cache'
        }

        if ($cacheHome) {
            $playwrightCacheRoots += Join-Path $cacheHome 'ms-playwright'
        }
    }

    foreach ($playwrightCacheRoot in $playwrightCacheRoots | Where-Object { $_ } | Select-Object -Unique) {
        if (-not (Test-Path -LiteralPath $playwrightCacheRoot)) {
            continue
        }

        $candidates += Get-ChildItem -Path $playwrightCacheRoot -Directory -Filter 'chromium-*' -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object {
                @(
                    (Join-Path $_.FullName 'chrome-linux/chrome'),
                    (Join-Path $_.FullName 'chrome-win64/chrome.exe'),
                    (Join-Path $_.FullName 'chrome-win/chrome.exe'),
                    (Join-Path $_.FullName 'chrome-mac/Chromium.app/Contents/MacOS/Chromium')
                )
            }
    }

    foreach ($commandName in @('google-chrome', 'chrome', 'chromium', 'chromium-browser', 'msedge', 'microsoft-edge')) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue
        if ($command) {
            $candidates += $command.Source
        }
    }

    return $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

function Start-StaticFileServer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    $stopFile = Join-Path ([System.IO.Path]::GetTempPath()) "ntcomponents-aot-smoke-$Port.stop"
    if (Test-Path $stopFile) {
        Remove-Item -LiteralPath $stopFile -Force
    }

    $job = Start-Job -ArgumentList $Root, $Port, $stopFile -ScriptBlock {
        param($Root, $Port, $StopFile)

        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Parse('127.0.0.1'), $Port)
        $listener.Start()

        try {
            while (-not (Test-Path $StopFile)) {
                if (-not $listener.Pending()) {
                    Start-Sleep -Milliseconds 50
                    continue
                }

                $client = $listener.AcceptTcpClient()
                $client.ReceiveTimeout = 5000
                try {
                    $stream = $client.GetStream()
                    $buffer = [byte[]]::new(8192)
                    $read = $stream.Read($buffer, 0, $buffer.Length)
                    if ($read -le 0) {
                        continue
                    }

                    $requestText = [System.Text.Encoding]::ASCII.GetString($buffer, 0, $read)
                    $requestLine = ($requestText -split "\r?\n", 2)[0]
                    if ($requestLine -notmatch '^(GET|HEAD)\s+([^ ]+)') {
                        $response = [System.Text.Encoding]::ASCII.GetBytes("HTTP/1.1 400 Bad Request`r`nConnection: close`r`nContent-Length: 0`r`n`r`n")
                        $stream.Write($response, 0, $response.Length)
                        continue
                    }

                    $method = $Matches[1]
                    $requestUri = [Uri]::new("http://127.0.0.1$($Matches[2])")
                    $requestPath = [Uri]::UnescapeDataString($requestUri.AbsolutePath.TrimStart('/'))
                    if ([string]::IsNullOrWhiteSpace($requestPath)) {
                        $requestPath = 'index.html'
                    }

                    $relativePath = $requestPath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
                    $fullPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($Root, $relativePath))
                    $rootPath = [System.IO.Path]::GetFullPath($Root)

                    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
                        $response = [System.Text.Encoding]::ASCII.GetBytes("HTTP/1.1 403 Forbidden`r`nConnection: close`r`nContent-Length: 0`r`n`r`n")
                        $stream.Write($response, 0, $response.Length)
                        continue
                    }

                    if (-not (Test-Path $fullPath)) {
                        $fullPath = [System.IO.Path]::Combine($rootPath, 'index.html')
                    }

                    if (-not (Test-Path $fullPath)) {
                        $response = [System.Text.Encoding]::ASCII.GetBytes("HTTP/1.1 404 Not Found`r`nConnection: close`r`nContent-Length: 0`r`n`r`n")
                        $stream.Write($response, 0, $response.Length)
                        continue
                    }

                    $extension = [System.IO.Path]::GetExtension($fullPath).ToLowerInvariant()
                    $contentType = switch ($extension) {
                        '.html' { 'text/html' }
                        '.js' { 'text/javascript' }
                        '.mjs' { 'text/javascript' }
                        '.css' { 'text/css' }
                        '.json' { 'application/json' }
                        '.wasm' { 'application/wasm' }
                        '.dat' { 'application/octet-stream' }
                        default { 'application/octet-stream' }
                    }

                    $bytes = [System.IO.File]::ReadAllBytes($fullPath)
                    $headers = [System.Text.Encoding]::ASCII.GetBytes("HTTP/1.1 200 OK`r`nContent-Type: $contentType`r`nContent-Length: $($bytes.Length)`r`nCache-Control: no-store`r`nConnection: close`r`n`r`n")
                    $stream.Write($headers, 0, $headers.Length)
                    if ($method -ne 'HEAD') {
                        $stream.Write($bytes, 0, $bytes.Length)
                    }
                }
                catch {
                    # Drop a connection that times out or resets so one idle socket cannot stall the server.
                }
                finally {
                    $client.Close()
                }
            }
        }
        finally {
            $listener.Stop()
        }
    }

    return @{
        Job = $job
        StopFile = $stopFile
        Url = "http://127.0.0.1:$Port/"
    }
}

function Stop-StaticFileServer {
    param(
        [Parameter(Mandatory = $true)]
        $Server
    )

    New-Item -Path $Server.StopFile -ItemType File -Force | Out-Null
    Wait-Job $Server.Job -Timeout 5 | Out-Null
    Stop-Job $Server.Job -ErrorAction SilentlyContinue
    Remove-Job $Server.Job -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $Server.StopFile -Force -ErrorAction SilentlyContinue
}

function Invoke-ChromiumRuntimeExpression {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WebSocketUrl,

        [Parameter(Mandatory = $true)]
        [string]$Expression
    )

    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    $cancellation = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(10))

    try {
        $socket.ConnectAsync([Uri]::new($WebSocketUrl), $cancellation.Token).GetAwaiter().GetResult()

        $message = @{
            id = 1
            method = 'Runtime.evaluate'
            params = @{
                expression = $Expression
                returnByValue = $true
                awaitPromise = $true
            }
        } | ConvertTo-Json -Compress -Depth 5

        $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
        $socket.SendAsync([ArraySegment[byte]]::new($messageBytes), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $cancellation.Token).GetAwaiter().GetResult()

        $buffer = [byte[]]::new(65536)
        $builder = [System.Text.StringBuilder]::new()

        while ($true) {
            $result = $socket.ReceiveAsync([ArraySegment[byte]]::new($buffer), $cancellation.Token).GetAwaiter().GetResult()
            if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
                throw "Chromium DevTools socket closed before returning an evaluation result."
            }

            [void]$builder.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $result.Count))
            if (-not $result.EndOfMessage) {
                continue
            }

            $responseText = $builder.ToString()
            [void]$builder.Clear()
            $response = $responseText | ConvertFrom-Json
            if ($response.id -ne 1) {
                continue
            }

            if ($response.error) {
                throw "Chromium DevTools evaluation failed: $($response.error.message)"
            }

            return $response.result.result.value
        }
    }
    finally {
        $cancellation.Dispose()
        if ($socket.State -eq [System.Net.WebSockets.WebSocketState]::Open) {
            try {
                if (-not $socket.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'done', [System.Threading.CancellationToken]::None).Wait(5000)) {
                    $socket.Abort()
                }
            }
            catch {
                $socket.Abort()
            }
        }

        $socket.Dispose()
    }
}

function Start-BrowserProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Browser,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $Browser
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardError = $true
    $startInfo.RedirectStandardOutput = $true

    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
    $standardErrorTask = $process.StandardError.ReadToEndAsync()

    return [pscustomobject]@{
        Process = $process
        StandardOutputTask = $standardOutputTask
        StandardErrorTask = $standardErrorTask
    }
}

function Get-BrowserProcessOutput {
    param(
        [Parameter(Mandatory = $true)]
        $BrowserRun
    )

    $standardOutput = ''
    $standardError = ''

    try {
        if ($BrowserRun.StandardOutputTask.Wait(1000)) {
            $standardOutput = $BrowserRun.StandardOutputTask.Result
        }
    }
    catch {
        $standardOutput = "Failed to read Chromium stdout: $($_.Exception.Message)"
    }

    try {
        if ($BrowserRun.StandardErrorTask.Wait(1000)) {
            $standardError = $BrowserRun.StandardErrorTask.Result
        }
    }
    catch {
        $standardError = "Failed to read Chromium stderr: $($_.Exception.Message)"
    }

    $output = @()
    if (-not [string]::IsNullOrWhiteSpace($standardOutput)) {
        $output += "stdout:`n$standardOutput"
    }

    if (-not [string]::IsNullOrWhiteSpace($standardError)) {
        $output += "stderr:`n$standardError"
    }

    if ($output.Count -eq 0) {
        return 'Chromium did not write stdout or stderr.'
    }

    return $output -join "`n"
}

function Test-PublishedOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PublishWwwRoot
    )

    $indexPath = Join-Path $PublishWwwRoot 'index.html'
    $frameworkDirectory = Join-Path $PublishWwwRoot '_framework'
    $wasmFiles = Get-ChildItem -Path $frameworkDirectory -Filter '*.wasm' -ErrorAction SilentlyContinue

    if (-not (Test-Path $indexPath)) {
        throw "Published output is missing index.html: $indexPath"
    }

    if (-not $wasmFiles) {
        throw "Published output is missing WebAssembly payloads under: $frameworkDirectory"
    }
}

function Invoke-BrowserSmoke {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PublishWwwRoot
    )

    $browser = Get-BrowserExecutable
    if (-not $browser) {
        throw "No Chromium-based browser was found for the AOT smoke test."
    }

    $port = Get-FreePort
    $server = Start-StaticFileServer -Root $PublishWwwRoot -Port $port

    try {
        $ready = $false
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try {
                $null = Invoke-WebRequest -Uri $server.Url -UseBasicParsing -TimeoutSec 5
                $ready = $true
                break
            }
            catch {
                Start-Sleep -Milliseconds 250
            }
        }

        if (-not $ready) {
            throw "Static file server did not become ready at $($server.Url)."
        }

        $lastDom = 'DOM was not captured.'
        $lastBrowserExitCode = $null
        $lastBrowserOutput = 'Chromium output was not captured.'
        $lastBrowserError = 'Chromium DevTools did not report an error.'
        foreach ($attempt in 1..3) {
            $debugPort = Get-FreePort
            $browserUserDataDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "ntcomponents-aot-browser-$Port-$attempt"
            Remove-Item -LiteralPath $browserUserDataDirectory -Recurse -Force -ErrorAction SilentlyContinue

            $browserArgs = @(
                '--headless=new',
                '--disable-gpu',
                '--disable-gpu-compositing',
                '--disable-gpu-rasterization',
                '--disable-gpu-shader-disk-cache',
                '--disable-accelerated-2d-canvas',
                '--disable-accelerated-video-decode',
                '--disable-features=CalculateNativeWinOcclusion,UseSkiaRenderer,SkiaGraphite,SkiaGraphiteUsePersistentCache,GpuPersistentCache,WebGPU,Vulkan',
                '--disable-dev-shm-usage',
                '--no-first-run',
                '--disable-background-networking',
                '--disable-search-engine-choice-screen',
                '--use-mock-keychain',
                '--password-store=basic',
                '--remote-allow-origins=*',
                "--remote-debugging-port=$debugPort",
                "--user-data-dir=$browserUserDataDirectory",
                $server.Url
            )

            if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) {
                $browserArgs = @(
                    '--no-sandbox',
                    '--disable-setuid-sandbox',
                    '--disable-crash-reporter',
                    '--disable-crashpad',
                    '--disable-breakpad'
                ) + $browserArgs
            }

            if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
                $browserArgs = @('--disable-gpu-sandbox') + $browserArgs
            }

            Write-Host "Browser smoke attempt ${attempt}: launching $browser against $($server.Url)"
            $browserRun = Start-BrowserProcess -Browser $browser -Arguments $browserArgs
            $browserProcess = $browserRun.Process

            try {
                $devToolsPage = $null
                $deadline = [DateTimeOffset]::UtcNow.AddSeconds(120)
                while ([DateTimeOffset]::UtcNow -lt $deadline) {
                    if ($browserProcess.HasExited) {
                        $lastBrowserExitCode = $browserProcess.ExitCode
                        break
                    }

                    try {
                        $devToolsPages = Invoke-RestMethod -Uri "http://127.0.0.1:$debugPort/json" -TimeoutSec 5
                        $devToolsPage = @($devToolsPages) | Where-Object { $_.url -eq $server.Url -and $_.webSocketDebuggerUrl } | Select-Object -First 1
                        if ($devToolsPage) {
                            break
                        }

                        Start-Sleep -Milliseconds 250
                    }
                    catch {
                        Start-Sleep -Milliseconds 250
                    }
                }

                if ($devToolsPage) {
                    Write-Host "Browser smoke attempt ${attempt}: DevTools connected, waiting for the smoke marker"
                    while ([DateTimeOffset]::UtcNow -lt $deadline) {
                        if ($browserProcess.HasExited) {
                            $lastBrowserExitCode = $browserProcess.ExitCode
                            break
                        }

                        $isReady = Invoke-ChromiumRuntimeExpression -WebSocketUrl $devToolsPage.webSocketDebuggerUrl -Expression '(() => { const ready = document.querySelector("[data-aot-smoke-ready=\"true\"]") !== null && document.getElementById("aot-smoke-status") !== null; const gridSorted = document.querySelector("[data-aot-smoke-row]")?.getAttribute("data-aot-smoke-row") === "2"; const smokeWindow = document.querySelector("[data-aot-smoke-window=\"true\"]"); const toast = document.querySelector(".nt-toast"); const snackbar = document.querySelector(".nt-snackbar"); return ready && gridSorted && smokeWindow?.textContent === "AOT window content" && toast?.querySelector(".nt-toast-title")?.textContent === "AOT toast title" && toast?.querySelector(".nt-toast-message")?.textContent === "AOT toast message" && toast?.classList.contains("nt-toast-success") === true && toast?.style.getPropertyValue("--nt-toast-background-color").trim() === "var(--tnt-color-primary-container)" && toast?.style.getPropertyValue("--nt-toast-text-color").trim() === "var(--tnt-color-on-primary-container)" && snackbar?.querySelector(".nt-snackbar-message")?.textContent === "AOT snackbar message" && snackbar?.querySelector(".nt-snackbar-action")?.textContent === "Undo" && snackbar?.style.getPropertyValue("--nt-snackbar-background-color").trim() === "var(--tnt-color-secondary-container)" && snackbar?.style.getPropertyValue("--nt-snackbar-text-color").trim() === "var(--tnt-color-on-secondary-container)" && snackbar?.style.getPropertyValue("--nt-snackbar-action-color").trim() === "var(--tnt-color-tertiary)"; })()'
                        if ($isReady) {
                            Remove-Item -LiteralPath $browserUserDataDirectory -Recurse -Force -ErrorAction SilentlyContinue
                            return
                        }

                        $lastDom = Invoke-ChromiumRuntimeExpression -WebSocketUrl $devToolsPage.webSocketDebuggerUrl -Expression 'document.documentElement.outerHTML'
                        Start-Sleep -Milliseconds 500
                    }
                }
            }
            catch {
                $lastBrowserError = $_.Exception.Message
            }
            finally {
                if ($browserProcess -and -not $browserProcess.HasExited) {
                    $browserProcess.Kill($true)
                    [void]$browserProcess.WaitForExit(5000)
                }

                $lastBrowserOutput = Get-BrowserProcessOutput -BrowserRun $browserRun
                $browserProcess.Dispose()
            }

            Write-Host "Browser smoke attempt ${attempt}: did not render the smoke marker (exit code: $lastBrowserExitCode, DevTools error: $lastBrowserError)"
            Remove-Item -LiteralPath $browserUserDataDirectory -Recurse -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
        }

        if ($null -ne $lastBrowserExitCode -and $lastBrowserExitCode -ne 0) {
            throw "Browser smoke failed with exit code $lastBrowserExitCode. DevTools error: $lastBrowserError`nOutput:`n$lastDom`nChromium output:`n$lastBrowserOutput"
        }

        throw "Browser smoke did not render the NTComponents AOT smoke marker. DevTools error: $lastBrowserError`nOutput:`n$lastDom`nChromium output:`n$lastBrowserOutput"
    }
    finally {
        Stop-StaticFileServer -Server $server
    }
}

$analysisWarningCount = 0
$failedRuns = 0

foreach ($framework in $targetFrameworks) {
    Write-Host "--- Publishing Blazor WebAssembly AOT smoke app for framework: $framework ---"

    $publishDirectory = Join-Path $rootDirectory "Tests/AotCompatibility.TestApp/bin/Release/$framework/publish"
    $publishWwwRoot = Join-Path $publishDirectory 'wwwroot'

    if (Test-Path $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }

    $publishArgs = @(
        $projectPath,
        '-nodeReuse:false',
        '/p:UseSharedCompilation=false',
        '-c', 'Release',
        '-f', $framework
    )

    $publishOutput = & dotnet publish @publishArgs 2>&1 | ForEach-Object {
        Write-Host $_
        $_
    }
    $publishExitCode = $LASTEXITCODE

    $warningLines = $publishOutput | Where-Object { $_ -match '\bIL(2|3)\d{3}\b' -and $_ -match 'warning' }
    foreach ($line in $warningLines) {
        Write-Host "AOT/trim analysis warning: $line"
        $analysisWarningCount += 1
    }

    if ($publishExitCode -ne 0) {
        Write-Host "Publish failed for framework $framework with exit code $publishExitCode."
        $failedRuns += 1
        continue
    }

    try {
        Test-PublishedOutput -PublishWwwRoot $publishWwwRoot

        if (-not $SkipBrowserSmoke) {
            Invoke-BrowserSmoke -PublishWwwRoot $publishWwwRoot
            Write-Host "Browser smoke finished successfully for framework $framework."
        }
    }
    catch {
        Write-Host "Validation failed for framework ${framework}: $_"
        $failedRuns += 1
    }
}

Write-Host "AOT/trim analysis warning count is: $analysisWarningCount"
Write-Host "Failed run count is: $failedRuns"

if ($analysisWarningCount -ne 0 -or $failedRuns -ne 0) {
    Write-Host "AOT compatibility test FAILED. Warnings or failed runs detected."
    Exit 1
}

if ($SkipBrowserSmoke) {
    Write-Host "AOT compatibility test PASSED. No analysis warnings and all publish checks succeeded. Browser smoke was skipped."
}
else {
    Write-Host "AOT compatibility test PASSED. No analysis warnings and all publish/browser checks succeeded."
}
Exit 0
