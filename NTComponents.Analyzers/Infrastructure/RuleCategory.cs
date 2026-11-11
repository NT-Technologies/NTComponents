namespace NTComponents.Analyzers;

/// <summary>Diagnostic categories. Release tracking requires these to be constants.</summary>
internal static class RuleCategory {
    public const string Usage = "Usage";
    public const string Accessibility = "Accessibility";
    public const string Migration = "Migration";

    /// <summary>Each rule links to its RULES.md section: <c>HelpUri + "#ntc1003"</c>.</summary>
    public const string HelpUri = "https://github.com/NT-Technologies/NTComponents/blob/main/NTComponents.Analyzers/RULES.md";
}
