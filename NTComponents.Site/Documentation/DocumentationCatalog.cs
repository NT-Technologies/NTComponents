using Microsoft.AspNetCore.Components;
using NTComponents.GeneratedDocumentation;
using NTComponents.Site.Examples;
using System.Text;
using System.Text.RegularExpressions;

namespace NTComponents.Site.Documentation;

public sealed partial class DocumentationCatalog {
    private static readonly IReadOnlyDictionary<string, string> DependentComponentParentsByName = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["NTAccordionItem"] = "NTAccordion",
        ["NTAutocompleteOption"] = "NTAutocomplete",
        ["NTAutocompleteOptionGroup"] = "NTAutocomplete",
        ["NTButtonGroupItem"] = "NTButtonGroup",
        ["NTBody"] = "NTLayout",
        ["NTCarouselItem"] = "NTCarousel",
        ["NTFabMenuAnchorItem"] = "NTFabMenu",
        ["NTFabMenuButtonItem"] = "NTFabMenu",
        ["NTFileUploadItem"] = "NTFileUpload",
        ["NTFooter"] = "NTLayout",
        ["NTHeader"] = "NTLayout",
        ["NTInputRadio"] = "NTInputRadioGroup",
        ["NTMenuAnchorItem"] = "NTMenu",
        ["NTMenuButtonItem"] = "NTMenu",
        ["NTMenuDividerItem"] = "NTMenu",
        ["NTMenuLabelItem"] = "NTMenu",
        ["NTMenuSubMenuItem"] = "NTMenu",
        ["NTNavigationRailGroup"] = "NTNavigationRail",
        ["NTNavigationRailItem"] = "NTNavigationRail",
        ["NTNavigationRailSectionHeader"] = "NTNavigationRail",
        ["NTPageScript"] = "NTHeadDependencies",
        ["NTPopoutTab"] = "NTTabView",
        ["NTPropertyColumn"] = "NTDataGrid",
        ["NTTab"] = "NTTabView",
        ["NTTemplateColumn"] = "NTDataGrid",
        ["NTWizardFormStep"] = "NTWizard",
        ["NTWizardStep"] = "NTWizard",

        // Charts dependents are keyed by full name because NTComponents.Charts.Core.NTTooltip shares its name with the core NTTooltip.
        ["NTComponents.Charts.Core.NTLegend"] = "NTChart",
        ["NTComponents.Charts.Core.NTTooltip"] = "NTChart",
        ["NTComponents.Charts.Core.Axes.NTXAxisOptions"] = "NTChart",
        ["NTComponents.Charts.Core.Axes.NTYAxisOptions"] = "NTChart",
        ["NTComponents.Charts.Core.Axes.NTRadialAxisOptions"] = "NTRadarSeries"
    };

    private readonly IReadOnlyDictionary<string, ComponentDocumentationEntry> _componentRoutes;
    private readonly IReadOnlyList<ReferenceDocumentationEntry> _references;

    public DocumentationCatalog() {
        var componentEntries = AllTypes
            .Where(IsDocumentedNtComponent)
            .Select(type => new ComponentDocumentationEntry(type, CreateComponentSlug(type), ResolveGroupName(type), BuildComponentDisplayName(type.Name), BuildMemberGroups(type), ComponentExamples.Pages.GetValueOrDefault(type.Name)))
            .ToList();
        var referencedTypeNames = componentEntries
            .SelectMany(component => component.Parameters)
            .SelectMany(parameter => GetReferencedTypeNames(parameter.TypeFullName))
            .ToHashSet(StringComparer.Ordinal);

        Components = [.. BuildComponentPages(componentEntries)
            .OrderBy(component => component.GroupName, StringComparer.Ordinal)
            .ThenBy(component => component.DisplayName, StringComparer.Ordinal)];

        _componentRoutes = Components
            .SelectMany(page => page.DependentComponents.Prepend(page).Select(component => (component.Slug, page)))
            .ToDictionary(route => route.Slug, route => route.page, StringComparer.OrdinalIgnoreCase);

        _references = [.. AllTypes
            .Where(type => IsReferenceType(type, referencedTypeNames))
            .Select(type => new ReferenceDocumentationEntry(type, CreateSlug(type.FullName), BuildReferenceGroupName(type), ResolveGroupName(type)))
            .OrderBy(reference => reference.GroupName, StringComparer.Ordinal)
            .ThenBy(reference => reference.FamilyName, StringComparer.Ordinal)
            .ThenBy(reference => reference.Type.Name, StringComparer.Ordinal)];
    }

    // The core library and the Charts library are documented by separate generated models that share the same model types.
    private static IEnumerable<TypeDocumentation> AllTypes =>
        GeneratedCodeDocumentation.Model.Types.Concat(NTComponents.Charts.GeneratedDocumentation.GeneratedCodeDocumentation.Model.Types);

    public IReadOnlyList<ComponentDocumentationEntry> Components { get; }

    public int DocumentedComponentTypeCount => Components.Sum(component => 1 + component.DependentComponents.Count);

    public IEnumerable<IGrouping<string, ComponentDocumentationEntry>> ComponentGroups =>
        Components.GroupBy(component => component.GroupName).OrderBy(group => group.Key, StringComparer.Ordinal);

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> ReferenceGroups =>
        _references.GroupBy(reference => reference.GroupName).OrderBy(group => group.Key, StringComparer.Ordinal);

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> EnumGroups =>
        GetReferenceGroups("Enums");

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> ConstantGroups =>
        GetReferenceGroups("Constants");

    public ComponentDocumentationEntry? GetComponent(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : _componentRoutes.GetValueOrDefault(slug);

    public ReferenceDocumentationEntry? GetReference(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : _references.FirstOrDefault(reference => string.Equals(reference.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IGrouping<string, ReferenceDocumentationEntry>> GetReferenceGroups(string groupName) =>
        _references
            .Where(reference => string.Equals(reference.GroupName, groupName, StringComparison.Ordinal))
            .GroupBy(reference => reference.FamilyName)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

    public string? GetTypeUrl(string? typeFullName) {
        if (string.IsNullOrWhiteSpace(typeFullName)) {
            return null;
        }

        var normalizedTypeFullName = NormalizeDocumentationReference(typeFullName);
        return ComponentMatches.FirstOrDefault(match => RemoveGenericArity(match.Component.Type.FullName) == normalizedTypeFullName) is { } componentMatch
            ? $"/components/{componentMatch.Page.Slug}{(componentMatch.IsDependent ? $"#{componentMatch.Component.Slug}" : string.Empty)}"
            : _references.FirstOrDefault(reference => reference.Type.FullName == normalizedTypeFullName) is { } reference
                ? $"/reference/{reference.RouteKind}/{reference.Slug}"
                : null;
    }

    public DocumentationLink? GetDocumentationLink(string? cref) {
        if (string.IsNullOrWhiteSpace(cref)) {
            return null;
        }

        var normalizedReference = NormalizeDocumentationReference(cref);
        var directTypeUrl = GetTypeUrl(normalizedReference);
        if (directTypeUrl is not null) {
            return new DocumentationLink(directTypeUrl, ShortTypeName(normalizedReference));
        }

        var componentMatch = ComponentMatches
            .Where(match => normalizedReference.StartsWith(RemoveGenericArity(match.Component.Type.FullName) + ".", StringComparison.Ordinal))
            .OrderByDescending(match => RemoveGenericArity(match.Component.Type.FullName).Length)
            .FirstOrDefault();
        if (componentMatch is not null) {
            var memberName = normalizedReference[(RemoveGenericArity(componentMatch.Component.Type.FullName).Length + 1)..];
            var anchor = componentMatch.IsDependent ? $"{componentMatch.Component.Slug}-{CreateSlug(memberName)}" : CreateSlug(memberName);
            return new DocumentationLink($"/components/{componentMatch.Page.Slug}#{anchor}", memberName);
        }

        var reference = _references
            .Where(reference => normalizedReference.StartsWith(reference.Type.FullName + ".", StringComparison.Ordinal))
            .OrderByDescending(reference => reference.Type.FullName.Length)
            .FirstOrDefault();
        if (reference is not null) {
            var memberName = normalizedReference[(reference.Type.FullName.Length + 1)..];
            return new DocumentationLink($"/reference/{reference.RouteKind}/{reference.Slug}#{CreateSlug(memberName)}", memberName);
        }

        return null;
    }

    public IEnumerable<ComponentDocumentationEntry> SearchComponents(string? query) {
        if (string.IsNullOrWhiteSpace(query)) {
            return Components;
        }

        var terms = query.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return Components.Where(component => terms.All(term => MatchesTerm(component, term) || component.DependentComponents.Any(child => MatchesTerm(child, term))));
    }

    private static IReadOnlyList<ComponentDocumentationEntry> BuildComponentPages(IReadOnlyList<ComponentDocumentationEntry> componentEntries) {
        var names = componentEntries.Select(component => component.Type.Name).ToHashSet(StringComparer.Ordinal);
        bool IsDependent(ComponentDocumentationEntry component) => TryGetParentName(component.Type, out var parentName) && names.Contains(parentName);

        var childrenByParentName = componentEntries.Where(IsDependent).ToLookup(component => TryGetParentName(component.Type, out var parentName) ? parentName : string.Empty, StringComparer.Ordinal);
        return [.. componentEntries
            .Where(component => !IsDependent(component))
            .Select(component => component with { DependentComponents = [.. childrenByParentName[component.Type.Name].OrderBy(child => child.DisplayName, StringComparer.Ordinal)] })];
    }

    private IEnumerable<ComponentDocumentationMatch> ComponentMatches =>
        Components.SelectMany(page => page.DependentComponents
            .Select(child => new ComponentDocumentationMatch(page, child, true))
            .Prepend(new ComponentDocumentationMatch(page, page, false)));

    private static bool MatchesTerm(ComponentDocumentationEntry component, string term) =>
        Contains(component.DisplayName, term) ||
        Contains(component.Type.FullName, term) ||
        Contains(component.Type.Summary, term) ||
        Contains(component.Type.Remarks, term) ||
        component.Parameters.Any(parameter => Contains(parameter.Name, term) || Contains(parameter.Summary, term));

    private static bool Contains(string? value, string term) => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsDocumentedNtComponent(TypeDocumentation documentation) =>
        documentation.Kind == "Class" &&
        documentation.Accessibility == "Public" &&
        !documentation.IsObsolete &&
        (ComponentExamples.Pages.ContainsKey(documentation.Name) || TryGetParentName(documentation, out _));

    private static bool TryGetParentName(TypeDocumentation type, out string parentName) =>
        DependentComponentParentsByName.TryGetValue(RemoveGenericArity(type.FullName), out parentName!) || DependentComponentParentsByName.TryGetValue(type.Name, out parentName!);

    private static bool IsChartsType(TypeDocumentation type) => type.FullName.StartsWith("NTComponents.Charts.", StringComparison.Ordinal);

    // Charts dependents get a prefix so their routes cannot collide with a core component of the same name.
    private static string CreateComponentSlug(TypeDocumentation type) =>
        CreateSlug(IsChartsType(type) && TryGetParentName(type, out _) ? $"Chart {type.Name}" : type.Name);

    private static bool IsReferenceType(TypeDocumentation type, HashSet<string> referencedTypeNames) =>
        referencedTypeNames.Contains(type.FullName) ||
        ((type.Kind == "Enum" || type.Fields.Any(field => IsPublicOrProtected(field.Accessibility) && !string.IsNullOrWhiteSpace(field.ConstantValue))));

    private static IEnumerable<string> GetReferencedTypeNames(string typeFullName) =>
        string.IsNullOrWhiteSpace(typeFullName)
            ? []
            : GenericTypeNameRegex().Matches(typeFullName)
                .Select(match => match.Value.TrimEnd('?', '[', ']'))
                .Where(value => value.StartsWith("NTComponents.", StringComparison.Ordinal))
                .Select(RemoveGenericArity);

    private static ComponentMemberGroups BuildMemberGroups(TypeDocumentation type) {
        return new ComponentMemberGroups(
            SortProperties(type.Properties.Where(member => !member.IsFromBaseType && IsPublicOrProtected(member.Accessibility))),
            SortProperties(type.Properties.Where(member => member.IsFromBaseType && IsPublicOrProtected(member.Accessibility))),
            SortFields(type.Fields.Where(member => !member.IsFromBaseType && IsPublicOrProtected(member.Accessibility))),
            SortFields(type.Fields.Where(member => member.IsFromBaseType && IsPublicOrProtected(member.Accessibility))));
    }

    private static string BuildReferenceGroupName(TypeDocumentation type) =>
        type.Kind == "Enum" ? "Enums" : "Constants";

    private static string ResolveGroupName(TypeDocumentation type) =>
        IsChartsType(type) ? "Charts" : string.IsNullOrWhiteSpace(type.SourceFolder) ? "Core" : type.SourceFolder switch {
            "NavRail" => "Navigation",
            "Views" => "Layout",
            _ => type.SourceFolder
        };

    private static string BuildComponentDisplayName(string name) => name.StartsWith("NT", StringComparison.Ordinal) && name.Length > 2 ? name[2..] : name;

    public static string CreateSlug(string value) {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;
        foreach (var character in value) {
            if (char.IsLetterOrDigit(character)) {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0) {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static IReadOnlyList<PropertyDocumentation> SortProperties(IEnumerable<PropertyDocumentation> members) =>
        [.. members.OrderBy(member => member.Name, StringComparer.Ordinal).ThenBy(member => member.DeclaringTypeFullName, StringComparer.Ordinal)];

    private static IReadOnlyList<FieldDocumentation> SortFields(IEnumerable<FieldDocumentation> members) =>
        [.. members.OrderBy(member => member.Name, StringComparer.Ordinal).ThenBy(member => member.DeclaringTypeFullName, StringComparer.Ordinal)];

    public static bool IsPublicOrProtected(string accessibility) =>
        accessibility is "Public" or "Protected" or "ProtectedOrInternal";

    private static string RemoveGenericArity(string value) {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }

    private static string NormalizeDocumentationReference(string value) {
        value = value.Trim();
        if (value.Length > 2 && value[1] == ':') {
            value = value[2..];
        }

        value = value.TrimEnd('?');
        return RemoveGenericArity(value);
    }

    private static string ShortTypeName(string fullName) {
        var index = fullName.LastIndexOf('.');
        return index < 0 ? fullName : fullName[(index + 1)..];
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_.]*(?:`[0-9]+)?")]
    private static partial Regex GenericTypeNameRegex();
}

public sealed record ComponentDocumentationEntry(
    TypeDocumentation Type,
    string Slug,
    string GroupName,
    string DisplayName,
    ComponentMemberGroups MemberGroups,
    RenderFragment? Examples,
    IReadOnlyList<ComponentDocumentationEntry>? DependentComponents = null) {
    public IReadOnlyList<ComponentDocumentationEntry> DependentComponents { get; init; } = DependentComponents ?? [];

    public IReadOnlyList<PropertyDocumentation> Parameters => Type.Parameters
        .Where(parameter => DocumentationCatalog.IsPublicOrProtected(parameter.Accessibility))
        .OrderByDescending(parameter => parameter.IsEditorRequired)
        .ThenBy(parameter => parameter.Name, StringComparer.Ordinal)
        .ToArray();

    public int RequiredParameterCount => Parameters.Count(parameter => parameter.IsEditorRequired);

    public int DirectMemberCount => MemberGroups.DirectProperties.Count + MemberGroups.DirectFields.Count;
}

public sealed record ReferenceDocumentationEntry(TypeDocumentation Type, string Slug, string GroupName, string FamilyName) {
    public string RouteKind => GroupName == "Constants" ? "constants" : "enums";

    public int MemberCount => Type.Fields.Count(member => DocumentationCatalog.IsPublicOrProtected(member.Accessibility));
}

public sealed record DocumentationLink(string Href, string Label);

public sealed record ComponentMemberGroups(
    IReadOnlyList<PropertyDocumentation> DirectProperties,
    IReadOnlyList<PropertyDocumentation> InheritedProperties,
    IReadOnlyList<FieldDocumentation> DirectFields,
    IReadOnlyList<FieldDocumentation> InheritedFields);

sealed record ComponentDocumentationMatch(ComponentDocumentationEntry Page, ComponentDocumentationEntry Component, bool IsDependent);
