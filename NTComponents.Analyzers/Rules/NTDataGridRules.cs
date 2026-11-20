using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace NTComponents.Analyzers;

/// <summary>Rules for NTDataGrid and NTPropertyColumn, mirroring the checks NTDataGrid.ValidateConfiguration throws for.</summary>
internal static class NTDataGridRules {

    public static readonly DiagnosticDescriptor DuplicateSource = new(
        "NTC1065",
        "NTDataGrid cannot use both Items and ItemsProvider",
        "NTDataGrid requires either Items or ItemsProvider, not both",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1065");

    public static readonly DiagnosticDescriptor MissingSource = new(
        "NTC1066",
        "NTDataGrid requires a data source",
        "NTDataGrid requires Items or ItemsProvider",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1066");

    public static readonly DiagnosticDescriptor VirtualizedPagination = new(
        "NTC1067",
        "NTDataGrid cannot combine virtualization and pagination",
        "NTDataGrid does not support using Virtualize and ShowPagination together",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1067");

    public static readonly DiagnosticDescriptor ComputedPropertySort = new(
        "NTC1068",
        "Computed aggregate properties cannot be translated reliably",
        "NTPropertyColumn Property '{0}' combines other instance members and may fail when sorting a database-backed IQueryable; use NTTemplateColumn with SortBy targeting mapped properties",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1068");

    public static readonly DiagnosticDescriptor PersistencePair = new(
        "NTC1075",
        "NTDataGrid PersistenceKey requires persistence to be enabled",
        "NTDataGrid PersistenceKey requires PersistPrerenderedItems",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1075");

    public static readonly DiagnosticDescriptor PersistenceIdentity = new(
        "NTC1076",
        "NTDataGrid persistence requires a key",
        "NTDataGrid persistence requires a non-empty PersistenceKey",
        RuleCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1076");

    public static readonly DiagnosticDescriptor StaleNavigationUri = new(
        "NTC1113",
        "Merge NTDataGrid QueryParameters into URLs this component builds",
        "NavigationManager.Uri does not include this NTDataGrid's page and sort; merge the grid's QueryParameters into URLs this component builds for the current page",
        RuleCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: RuleCategory.HelpUri + "#ntc1113");

    public static void Register(ComponentRuleRegistry registry) {
        registry.Describe(DuplicateSource, MissingSource, VirtualizedPagination, ComputedPropertySort, PersistencePair, PersistenceIdentity, StaleNavigationUri);
        registry.On("NTComponents.NTDataGrid`1", AnalyzeGrid);
        registry.On("NTComponents.NTPropertyColumn`2", AnalyzeColumn);
    }

    private static void AnalyzeGrid(ComponentContext context) {
        var frame = context.Frame;
        var hasItems = IsSupplied(frame, "Items");
        var hasItemsProvider = IsSupplied(frame, "ItemsProvider");
        if (hasItems == true && hasItemsProvider == true) {
            context.Report(DuplicateSource, frame.LocationOf("ItemsProvider"));
        }
        else if (hasItems == false && hasItemsProvider == false) {
            context.Report(MissingSource, frame.Location);
        }

        var virtualize = frame.GetBool("Virtualize");
        if (virtualize == true && frame.GetBool("ShowPagination") == true) {
            context.Report(VirtualizedPagination, frame.LocationOf("ShowPagination"));
        }

        // The component only records whether the parameters were supplied, not their values.
        if (frame.Has("PersistenceKey") && !frame.Has("PersistPrerenderedItems") && !frame.HasSplattedAttributes) {
            context.Report(PersistencePair, frame.LocationOf("PersistenceKey"));
        }

        if (frame.GetBool("PersistPrerenderedItems") == true && frame.GetStringState("PersistenceKey") == StringState.Empty) {
            context.Report(PersistenceIdentity, frame.LocationOf("PersistPrerenderedItems"));
        }

        // OnParametersSetAsync replaces a non-positive PageSize with 10, and ResolvedVirtualizationItemSize raises the row height to at least 1.
        if (frame.GetNumber("PageSize") is { } pageSize && pageSize <= 0) {
            context.Report(CommonRules.Adjusted, frame.LocationOf("PageSize"), "NTDataGrid PageSize", Format(pageSize), "10");
        }

        if (virtualize == true && frame.GetNumber("VirtualizationItemSize") is { } itemSize && itemSize < 1) {
            context.Report(CommonRules.Adjusted, frame.LocationOf("VirtualizationItemSize"), "NTDataGrid VirtualizationItemSize", Format(itemSize), "1");
        }

        // Only the component that renders the grid is checked; a grid and a URL writer in different components are not linked.
        if (frame.GetBool("PushHistoryEntries") == false && context.ContainingType is { } owner && BuildsUrlFromNavigationManager(owner, context.CancellationToken)) {
            context.Report(StaleNavigationUri, frame.Location);
        }
    }

    /// <summary>
    ///     True when <paramref name="type" /> reads <c>Uri</c> from one of its NavigationManager members or builds a URL from it with
    ///     <c>GetUriWithQueryParameter(s)</c>, and never reads a grid's <c>QueryParameters</c>. Members are matched by name.
    /// </summary>
    private static bool BuildsUrlFromNavigationManager(INamedTypeSymbol type, CancellationToken cancellationToken) {
        var names = NavigationManagerNames(type);
        var accesses = type.DeclaringSyntaxReferences.SelectMany(reference => reference.GetSyntax(cancellationToken).DescendantNodes().OfType<MemberAccessExpressionSyntax>());
        return names.Count > 0
            && !accesses.Any(access => access.Name.Identifier.ValueText == "QueryParameters")
            && accesses.Any(access => names.Contains(ReceiverName(access.Expression)) && access.Name.Identifier.ValueText switch {
                "Uri" or "GetUriWithQueryParameter" => true,
                // The overload that takes its own base URI does not read NavigationManager.Uri.
                "GetUriWithQueryParameters" => access.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 },
                _ => false
            });
    }

    /// <summary>Names of the NavigationManager fields, properties, and primary constructor parameters <paramref name="type" /> can use.</summary>
    private static HashSet<string> NavigationManagerNames(INamedTypeSymbol type) {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current is not null; current = current.BaseType) {
            foreach (var member in current.GetMembers()) {
                if (member switch { IPropertySymbol property => property.Type, IFieldSymbol field => field.Type, _ => null } is INamedTypeSymbol memberType && IsNavigationManager(memberType)) {
                    names.Add(member.Name);
                }
            }
        }

        foreach (var parameter in type.InstanceConstructors.SelectMany(static constructor => constructor.Parameters)) {
            if (parameter.Type is INamedTypeSymbol parameterType && IsNavigationManager(parameterType)) {
                names.Add(parameter.Name);
            }
        }

        return names;
    }

    private static bool IsNavigationManager(INamedTypeSymbol type) {
        for (var current = type; current is not null; current = current.BaseType) {
            if (ComponentFrame.MetadataNameOf(current) == "Microsoft.AspNetCore.Components.NavigationManager") {
                return true;
            }
        }

        return false;
    }

    private static string ReceiverName(ExpressionSyntax expression) => expression switch {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax } access => access.Name.Identifier.ValueText,
        _ => string.Empty
    };

    /// <summary>True when the parameter holds a value other than a constant null, false when it is omitted or null, and null when unknown.</summary>
    private static bool? IsSupplied(ComponentFrame frame, string name) {
        if (!frame.Attributes.TryGetValue(name, out var attribute)) {
            return frame.HasSplattedAttributes ? null : false;
        }

        return !OperationConstants.IsNull(attribute.Value);
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static void AnalyzeColumn(ComponentContext context) {
        var frame = context.Frame;
        if (frame.Attributes.TryGetValue("Property", out var attribute)
            && GetSelectedProperty(attribute.Value) is { } property
            && IsComputedAggregate(property, context.CancellationToken)) {
            context.Report(ComputedPropertySort, attribute.Location, property.Name);
        }
    }

    /// <summary>Returns the property a lambda such as <c>row => row.FullName</c> returns, or null for any other shape.</summary>
    private static IPropertySymbol? GetSelectedProperty(IOperation? operation) {
        operation = OperationConstants.Unwrap(operation);
        if (operation is IDelegateCreationOperation delegateCreation) {
            operation = OperationConstants.Unwrap(delegateCreation.Target);
        }

        if (operation is not IAnonymousFunctionOperation lambda) {
            return null;
        }

        var returned = lambda.Body.DescendantsAndSelf()
            .OfType<IReturnOperation>()
            .Where(static returnOperation => returnOperation.ReturnedValue is not null)
            .Select(static returnOperation => returnOperation.ReturnedValue!)
            .Take(2)
            .ToArray();

        return returned.Length == 1 && OperationConstants.Unwrap(returned[0]) is IPropertyReferenceOperation reference ? reference.Property : null;
    }

    /// <summary>
    ///     Heuristic: the getter reads at least two different instance properties or fields of its own type, which a database
    ///     provider cannot translate. Names are matched against the declaring type and its bases, so a local that shadows a
    ///     member is skipped.
    /// </summary>
    private static bool IsComputedAggregate(IPropertySymbol property, CancellationToken cancellationToken) {
        foreach (var reference in property.DeclaringSyntaxReferences) {
            if (reference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax declaration) {
                continue;
            }

            SyntaxNode? getter = declaration.ExpressionBody?.Expression;
            if (getter is null) {
                var accessor = declaration.AccessorList?.Accessors.FirstOrDefault(static candidate => candidate.IsKind(SyntaxKind.GetAccessorDeclaration));
                getter = accessor?.ExpressionBody?.Expression ?? (SyntaxNode?)accessor?.Body;
            }

            if (getter is null) {
                continue;
            }

            var dependencies = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var shadowedNames = new HashSet<string>(
                getter.DescendantNodes(static node => !IsNestedExecutable(node)).OfType<VariableDeclaratorSyntax>().Select(static variable => variable.Identifier.ValueText),
                StringComparer.Ordinal);

            foreach (var identifier in getter.DescendantNodesAndSelf(static node => !IsNestedExecutable(node)).OfType<IdentifierNameSyntax>()) {
                var name = identifier.Identifier.ValueText;
                if (shadowedNames.Contains(name) || !IsDirectMemberIdentifier(identifier)) {
                    continue;
                }

                for (var type = property.ContainingType; type is not null; type = type.BaseType) {
                    foreach (var member in type.GetMembers(name)) {
                        if (member is IPropertySymbol or IFieldSymbol && !member.IsStatic && !SymbolEqualityComparer.Default.Equals(member, property) && dependencies.Add(member) && dependencies.Count >= 2) {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool IsNestedExecutable(SyntaxNode node) => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;

    private static bool IsDirectMemberIdentifier(IdentifierNameSyntax identifier) =>
        identifier.Parent is not MemberAccessExpressionSyntax memberAccess || memberAccess.Name != identifier || memberAccess.Expression is ThisExpressionSyntax or BaseExpressionSyntax;
}
