using NTComponents.Site.Documentation;

namespace NTComponents.Tests.Site;

public class NavigationMenuOrder_Tests {
    private static readonly DocumentationCatalog _catalog = new();

    public static TheoryData<string> Menus => ["Components", "Enums", "Constants"];

    [Theory]
    [MemberData(nameof(Menus))]
    public void MenuEntries_AreAlphabeticalByVisibleLabel(string menu) {
        // A family with one entry renders as that entry, so its visible label is the type name rather than the family name.
        var labels = GetMenu(menu).Select(group => group.Count() == 1 ? group.Single() : group.Key).ToArray();

        labels.Should().BeInAscendingOrder(SortKeyComparer, $"the {menu} menu should be alphabetical");
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void EntriesInsideEachFamily_AreAlphabetical(string menu) {
        foreach (var group in GetMenu(menu).Where(group => group.Count() > 1)) {
            group.Should().BeInAscendingOrder(SortKeyComparer, $"the {group.Key} family in the {menu} menu should be alphabetical");
        }
    }

    // The NT prefix is ignored so NTAccordion sorts under A next to plain names such as ButtonAppearance.
    private static readonly IComparer<string> SortKeyComparer = Comparer<string>.Create((left, right) => StringComparer.OrdinalIgnoreCase.Compare(StripPrefix(left), StripPrefix(right)));

    private static string StripPrefix(string name) => name.Length > 2 && name.StartsWith("NT", StringComparison.Ordinal) && char.IsUpper(name[2]) ? name[2..] : name;

    private static IEnumerable<IGrouping<string, string>> GetMenu(string menu) => menu switch {
        "Components" => _catalog.ComponentMenu.Select(group => Regroup(group.Key, group.Select(component => component.Type.Name))),
        "Enums" => _catalog.EnumMenu.Select(group => Regroup(group.Key, group.Select(reference => reference.Type.Name))),
        "Constants" => _catalog.ConstantMenu.Select(group => Regroup(group.Key, group.Select(reference => reference.Type.Name))),
        _ => throw new ArgumentOutOfRangeException(nameof(menu))
    };

    private static IGrouping<string, string> Regroup(string key, IEnumerable<string> names) => names.GroupBy(_ => key).Single();
}
