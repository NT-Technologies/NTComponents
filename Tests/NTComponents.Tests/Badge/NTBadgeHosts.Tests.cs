using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Badge;

public class NTBadgeHosts_Tests : BunitContext {

    public NTBadgeHosts_Tests() {
        Renderer.SetRendererInfo(new RendererInfo("Static", false));
        foreach (var path in new[] { "./_content/NTComponents/Menus/NTMenu.razor.js", "./_content/NTComponents/NavRail/NTNavigationRail.razor.js", "./_content/NTComponents/TabView/NTTabView.razor.js" }) {
            var module = JSInterop.SetupModule(path);
            module.SetupVoid("onLoad", _ => true).SetVoidResult();
            module.SetupVoid("onUpdate", _ => true).SetVoidResult();
            module.SetupVoid("onDispose", _ => true).SetVoidResult();
        }
    }

    [Fact]
    public void Button_Badge_Is_Part_Of_Content_Name_Without_Describedby() {
        var cut = Render<NTButton>(p => p
            .Add(x => x.Label, "Inbox")
            .Add(x => x.ShowBadge, true)
            .Add(x => x.BadgeContent, "3")
            .Add(x => x.BadgeAriaLabel, "3 unread messages"));

        var button = cut.Find("button");
        button.Children.Single(child => child.ClassList.Contains("nt-badge")).ClassList.Should().Contain("nt-badge-large").And.Contain("nt-badge-edge");
        button.HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Button_With_AriaLabel_Attribute_Describes_Badge() {
        var cut = Render<NTButton>(p => p
            .Add(x => x.Label, "Inbox")
            .Add(x => x.ShowBadge, true)
            .AddUnmatched("aria-label", "Open inbox"));

        cut.Find("button").GetAttribute("aria-describedby").Should().Be(cut.Find(".nt-badge").Id);
    }

    [Fact]
    public void Fab_Badge_Renders_In_Button_And_Describes_Icon_Only_Fab() {
        var cut = Render<NTFabButton>(p => p
            .Add(x => x.Icon, MaterialIcon.Edit)
            .Add(x => x.AriaLabel, "Compose")
            .Add(x => x.ShowBadge, true));

        var button = cut.Find("button");
        var badge = button.Children.Single(child => child.ClassList.Contains("nt-badge"));
        badge.ClassList.Should().Contain("nt-badge-small").And.Contain("nt-badge-edge");
        button.GetAttribute("aria-describedby").Should().Be(badge.Id);
    }

    [Fact]
    public void Chip_Badge_Renders_On_Root_And_Describes_Action() {
        var cut = Render<NTChip>(p => p
            .Add(x => x.Label, "Alerts")
            .Add(x => x.ShowBadge, true)
            .Add(x => x.BadgeContent, "2"));

        var root = cut.Find("span.nt-chip");
        var badge = root.Children.Single(child => child.ClassList.Contains("nt-badge"));
        badge.ClassList.Should().Contain("nt-badge-edge");
        cut.Find("button.nt-chip-control").GetAttribute("aria-describedby").Should().Be(badge.Id);
    }

    [Fact]
    public void Filter_Chip_Describes_Badge_From_Selection_Input() {
        var cut = Render<NTChip>(p => p
            .Add(x => x.Label, "Unread")
            .Add(x => x.Variant, NTChipVariant.Filter)
            .Add(x => x.ShowBadge, true));

        cut.Find("input.nt-chip-selection-input").GetAttribute("aria-describedby").Should().Be(cut.Find(".nt-badge").Id);
    }

    [Fact]
    public void Chip_Without_Badge_Has_No_Describedby() {
        var cut = Render<NTChip>(p => p.Add(x => x.Label, "Alerts"));

        cut.FindAll(".nt-badge").Should().BeEmpty();
        cut.Find("button.nt-chip-control").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Rail_Item_Badge_Shares_Content_Grid_Outside_Hidden_Icon() {
        var cut = RenderRailItem(item => item
            .Add(x => x.Label, "Inbox")
            .Add(x => x.Href, "/inbox")
            .Add(x => x.Icon, MaterialIcon.Mail)
            .Add(x => x.ShowBadge, true)
            .Add(x => x.BadgeContent, "3"));

        var content = cut.Find(".nt-navigation-rail-item-content");
        content.Children.Should().Contain(child => child.ClassList.Contains("nt-badge"));
        cut.Find(".nt-navigation-rail-item-icon").QuerySelector(".nt-badge").Should().BeNull();
        cut.Find("a.nt-navigation-rail-item").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Rail_Item_With_Distinct_AriaLabel_Describes_Badge() {
        var cut = RenderRailItem(item => item
            .Add(x => x.Label, "Inbox")
            .Add(x => x.AriaLabel, "Open inbox")
            .Add(x => x.Href, "/inbox")
            .Add(x => x.ShowBadge, true));

        cut.Find("a.nt-navigation-rail-item").GetAttribute("aria-describedby").Should().Be(cut.Find(".nt-badge").Id);
    }

    [Fact]
    public void Icon_Tab_Badge_Overlaps_Icon_And_Text_Tab_Badge_Is_Inline() {
        var cut = Render<NTTabView>(p => p.AddChildContent(builder => {
            builder.AddContent(0, Tab("Inbox", MaterialIcon.Mail, showBadge: true, badgeContent: "3"));
            builder.AddContent(1, Tab("Updates", null, showBadge: true, badgeContent: null));
        }));
        cut.WaitForAssertion(() => cut.FindAll(".nt-badge").Should().HaveCount(2));

        var tabs = cut.FindAll("[role='tab']");
        var iconBadge = tabs[0].QuerySelector(".nt-tab-view-content > .nt-badge")!;
        var textBadge = tabs[1].QuerySelector(".nt-tab-view-content > .nt-badge")!;
        iconBadge.ClassList.Should().Contain("nt-badge-large").And.NotContain("nt-badge-inline");
        textBadge.ClassList.Should().Contain("nt-badge-small").And.Contain("nt-badge-inline");
        tabs[1].QuerySelector(".nt-tab-view-content")!.ClassList.Should().Contain("nt-tab-view-text-only");
        tabs[0].HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Secondary_Icon_Tab_Badge_Is_Inline() {
        var cut = Render<NTTabView>(p => p
            .Add(x => x.Variant, NTTabViewVariant.Secondary)
            .AddChildContent(Tab("Inbox", MaterialIcon.Mail, showBadge: true, badgeContent: "3")));

        cut.WaitForAssertion(() => cut.Find(".nt-badge").ClassList.Should().Contain("nt-badge-inline"));
    }

    [Fact]
    public void Tab_Badge_Updates_When_Parameters_Change() {
        var cut = Render<NTTabView>(p => p.AddChildContent(Tab("Inbox", MaterialIcon.Mail, showBadge: false, badgeContent: null)));
        cut.WaitForAssertion(() => cut.FindAll("[role='tab']").Should().HaveCount(1));
        cut.FindAll(".nt-badge").Should().BeEmpty();

        cut.Render(p => p.AddChildContent(Tab("Inbox", MaterialIcon.Mail, showBadge: true, badgeContent: "5")));

        cut.WaitForAssertion(() => cut.Find(".nt-badge-label").TextContent.Should().Be("5"));
    }

    [Fact]
    public void Menu_Item_Badge_Is_Inline_And_Describes_Item() {
        var cut = Render<NTMenu>(p => p
            .Add(x => x.AriaLabel, "Mail")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Inbox")
                .Add(x => x.ShowBadge, true)
                .Add(x => x.BadgeContent, "3")
                .Add(x => x.BadgeAriaLabel, "3 unread messages")));

        var item = cut.Find("button.nt-menu-item");
        var badge = item.Children.Single(child => child.ClassList.Contains("nt-badge"));
        badge.ClassList.Should().Contain("nt-badge-inline");
        item.GetAttribute("aria-describedby").Should().Be(badge.Id);
    }

    [Fact]
    public void Sub_Menu_Item_Badge_Shares_Trailing_Column_With_Chevron() {
        var cut = Render<NTMenu>(p => p
            .Add(x => x.AriaLabel, "Mail")
            .AddChildContent<NTMenuSubMenuItem>(item => item
                .Add(x => x.Label, "Folders")
                .Add(x => x.ShowBadge, true)
                .Add(x => x.BadgeContent, "4")
                .AddChildContent<NTMenuButtonItem>(child => child.Add(x => x.Label, "Work"))));

        var trailing = cut.Find(".nt-menu-item-trailing");
        trailing.Children.Select(child => child.ClassList.Contains("nt-badge") ? "badge" : child.ClassName).Should().Equal("badge", "nt-menu-item-trailing-icon");
    }

    [Fact]
    public void Menu_Item_Badge_Updates_When_Parameters_Change() {
        var cut = Render<NTMenu>(p => p
            .Add(x => x.AriaLabel, "Mail")
            .AddChildContent<NTMenuButtonItem>(item => item.Add(x => x.Label, "Inbox")));
        cut.FindAll(".nt-badge").Should().BeEmpty();

        cut.Render(p => p
            .Add(x => x.AriaLabel, "Mail")
            .AddChildContent<NTMenuButtonItem>(item => item
                .Add(x => x.Label, "Inbox")
                .Add(x => x.ShowBadge, true)));

        cut.WaitForAssertion(() => cut.FindAll(".nt-badge").Should().ContainSingle());
    }

    private IRenderedComponent<NTNavigationRail> RenderRailItem(Action<ComponentParameterCollectionBuilder<NTNavigationRailItem>> item) =>
        Render<NTNavigationRail>(p => p
            .Add(x => x.AriaLabel, "Primary")
            .AddChildContent(item));

    private static RenderFragment Tab(string label, NTIcon? icon, bool showBadge, string? badgeContent) => builder => {
        builder.OpenComponent<NTTab>(0);
        builder.AddAttribute(1, nameof(NTTab.Label), label);
        builder.AddAttribute(2, nameof(NTTab.Icon), (object?)icon);
        builder.AddAttribute(3, nameof(NTTab.ShowBadge), showBadge);
        builder.AddAttribute(4, nameof(NTTab.BadgeContent), badgeContent);
        builder.AddAttribute(5, nameof(NTTab.ChildContent), (RenderFragment)(child => child.AddContent(0, $"{label} content")));
        builder.CloseComponent();
    };
}
