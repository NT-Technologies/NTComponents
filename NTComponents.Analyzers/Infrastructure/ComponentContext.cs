using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NTComponents.Analyzers;

/// <summary>Passed to a rule when a component frame closes, after all of its parameters and children are known.</summary>
internal readonly struct ComponentContext(ComponentFrame frame, SyntaxNodeAnalysisContext analysis) {
    public ComponentFrame Frame { get; } = frame;

    public CancellationToken CancellationToken => analysis.CancellationToken;

    /// <summary>The component class whose render tree contains this frame.</summary>
    public INamedTypeSymbol? ContainingType => analysis.ContainingSymbol as INamedTypeSymbol ?? analysis.ContainingSymbol?.ContainingType;

    public void Report(DiagnosticDescriptor rule, Location location, params object?[] messageArgs) => analysis.ReportDiagnostic(Diagnostic.Create(rule, location, messageArgs));

    /// <summary>Reports <paramref name="rule" /> with the parameter name for each color parameter that is a constant <c>Transparent</c>.</summary>
    public void ReportTransparent(DiagnosticDescriptor rule, string[] colorParameters) {
        foreach (var name in colorParameters) {
            if (Frame.GetEnumName(name) == "Transparent") {
                Report(rule, Frame.LocationOf(name), name);
            }
        }
    }
}
