using NTComponents.Site.Documentation;
using NTComponents.Site.Layout;

namespace NTComponents.Tests.Site;

public class NavigationIcons_Tests {
    private readonly DocumentationCatalog _catalog = new();

    [Fact]
    public void ComponentPages_EachHaveTheirOwnIcon() {
        var names = _catalog.Components.Select(component => component.Type.Name).ToArray();

        names.Should().OnlyContain(name => NavigationIcons.Contains(name), "every component page in the rail needs its own icon");
        names.Select(name => NavigationIcons.For(name).Icon).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ComponentGroups_HaveDistinctIcons() {
        var groupNames = _catalog.ComponentGroups.Where(group => group.Count() > 1).Select(group => group.Key).ToArray();

        groupNames.Should().OnlyContain(name => NavigationIcons.Contains(name));
        groupNames.Select(name => NavigationIcons.For(name).Icon).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ReferenceFamilies_AllHaveIcons() {
        var families = _catalog.EnumGroups.Concat(_catalog.ConstantGroups).Select(group => group.Key).Distinct();

        families.Should().OnlyContain(name => NavigationIcons.Contains(name), "reference families use their family icon in the rail");
    }
}
