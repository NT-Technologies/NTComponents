using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Carousel;

public class NTCarousel_Tests : BunitContext {

    public NTCarousel_Tests() {
        var module = JSInterop.SetupModule("./_content/NTComponents/Carousel/NTCarousel.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    [Fact, Trait("Component", "Carousel")]
    public void Renders_Accessible_Carousel_And_Slide_Semantics() {
        var cut = RenderCarousel(BuildItems(("Second", null), ("First", null)));

        var root = cut.Find("nt-carousel");
        root.GetAttribute("role").Should().Be("group");
        root.GetAttribute("aria-roledescription").Should().Be("carousel");
        root.GetAttribute("aria-label").Should().Be("Featured places");
        var items = cut.FindAll("nt-carousel-item");
        items.Select(item => item.TextContent.Trim()).Should().ContainInOrder("Second", "First");
        items[0].GetAttribute("aria-roledescription").Should().Be("slide");
        items[0].GetAttribute("aria-label").Should().Be("Second");
        items[0].GetAttribute("tabindex").Should().Be("0");
        items[1].GetAttribute("tabindex").Should().Be("0");
    }

    [Theory, Trait("Component", "Carousel")]
    [InlineData(CarouselAppearance.MultiBrowse, "multi-browse")]
    [InlineData(CarouselAppearance.Uncontained, "uncontained")]
    [InlineData(CarouselAppearance.UncontainedMultiAspectRatio, "uncontained-multi-aspect-ratio")]
    [InlineData(CarouselAppearance.Hero, "hero")]
    [InlineData(CarouselAppearance.CenterAlignedHero, "center-aligned-hero")]
    [InlineData(CarouselAppearance.FullScreen, "full-screen")]
    public void Renders_Every_Material_Layout(CarouselAppearance appearance, string expectedLayout) {
        var cut = RenderCarousel(BuildItems(("One", null)), appearance);

        cut.Find("nt-carousel").GetAttribute("data-layout").Should().Be(expectedLayout);
    }

    [Fact, Trait("Component", "Carousel")]
    public void Landmark_Uses_Named_Region() {
        var cut = Render<NTCarousel>(parameters => parameters
            .Add(component => component.AriaLabel, "Highlights")
            .Add(component => component.IsLandmark, true)
            .AddChildContent(BuildItems(("One", null))));

        cut.Find("nt-carousel").GetAttribute("role").Should().Be("region");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Autoplay_Renders_External_Pause_Control_And_Seconds_Contract() {
        var cut = Render<NTCarousel>(parameters => parameters
            .Add(component => component.AriaLabel, "Highlights")
            .Add(component => component.AutoPlayInterval, 4.5)
            .AddChildContent(BuildItems(("One", null), ("Two", null))));

        var root = cut.Find("nt-carousel");
        root.GetAttribute("data-auto-play-interval").Should().Be("4.5");
        var control = cut.Find(".nt-carousel-autoplay-control");
        control.TextContent.Trim().Should().Be("Pause rotation");
        control.ParentElement!.LocalName.Should().Be("nt-carousel");
        root.QuerySelector("[data-carousel-viewport] .nt-carousel-autoplay-control").Should().BeNull();
    }

    [Fact, Trait("Component", "Carousel")]
    public void Item_Renders_Independent_Media_And_Content_Layers() {
        RenderFragment items = builder => {
            builder.OpenComponent<NTCarouselItem>(0);
            builder.AddAttribute(1, nameof(NTCarouselItem.AriaLabel), "Lake");
            builder.AddAttribute(2, nameof(NTCarouselItem.BackgroundImageSrc), "lake.webp");
            builder.AddAttribute(3, nameof(NTCarouselItem.AspectRatio), 16d / 9d);
            builder.AddAttribute(4, nameof(NTCarouselItem.MediaContent), (RenderFragment)(media => media.AddMarkupContent(0, "<video muted></video>")));
            builder.AddAttribute(5, nameof(NTCarouselItem.ChildContent), (RenderFragment)(content => content.AddContent(0, "Lake content")));
            builder.CloseComponent();
        };

        var cut = RenderCarousel(items, CarouselAppearance.UncontainedMultiAspectRatio);

        cut.Find(".nt-carousel-item-media").GetAttribute("style").Should().Contain("background-image:url('lake.webp')");
        cut.Find(".nt-carousel-item-media video");
        cut.Find(".nt-carousel-item-content").TextContent.Should().Contain("Lake content");
        cut.Find("nt-carousel-item").GetAttribute("data-aspect-ratio").Should().Be((16d / 9d).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact, Trait("Component", "Carousel")]
    public void Click_Invokes_Item_Callback_And_Renders_Ripple() {
        var clicked = 0;
        RenderFragment items = builder => {
            builder.OpenComponent<NTCarouselItem>(0);
            builder.AddAttribute(1, nameof(NTCarouselItem.AriaLabel), "Clickable");
            builder.AddAttribute(2, nameof(NTCarouselItem.OnClickCallback), EventCallback.Factory.Create(this, () => clicked++));
            builder.AddAttribute(3, nameof(NTCarouselItem.ChildContent), (RenderFragment)(content => content.AddContent(0, "Open")));
            builder.CloseComponent();
        };
        var cut = RenderCarousel(items);

        cut.Find(".nt-carousel-item-content").Click();

        clicked.Should().Be(1);
        cut.Find("nt-carousel-item").InnerHtml.Should().Contain("nt-carousel-ripple-host");
        cut.Find("nt-carousel-item").GetAttribute("data-clickable").Should().Be("true");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Does_Not_Render_Embedded_Navigation_Controls() {
        var cut = RenderCarousel(BuildItems(("One", null), ("Two", null)));

        cut.FindAll("[data-previous], [data-next], .nt-carousel-indicator").Should().BeEmpty();
    }

    [Fact, Trait("Component", "Carousel")]
    public void Renders_Preferred_Item_Width_For_The_Responsive_Keyline_Engine() {
        var cut = Render<NTCarousel>(parameters => parameters
            .Add(component => component.AriaLabel, "Highlights")
            .Add(component => component.PreferredItemWidth, 224)
            .AddChildContent(BuildItems(("One", null))));

        cut.Find("nt-carousel").GetAttribute("data-preferred-item-width").Should().Be("224");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Rejects_Invalid_Preferred_Item_Width() {
        Action render = () => Render<NTCarousel>(parameters => parameters
            .Add(component => component.AriaLabel, "Invalid")
            .Add(component => component.PreferredItemWidth, 0)
            .AddChildContent(BuildItems(("One", null))));

        render.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory, Trait("Component", "Carousel")]
    [InlineData(null, null, 0)]
    [InlineData(null, 0, 240)]
    [InlineData(0d, null, 240)]
    public void Rejects_Invalid_Runtime_Configuration(double? autoPlayInterval, int? maxLargeItemWidth, int itemHeight) {
        Action render = () => Render<NTCarousel>(parameters => parameters
            .Add(component => component.AriaLabel, "Invalid")
            .Add(component => component.AutoPlayInterval, autoPlayInterval)
            .Add(component => component.MaxLargeItemWidth, maxLargeItemWidth)
            .Add(component => component.ItemHeight, itemHeight)
            .AddChildContent(BuildItems(("One", null))));

        render.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory, Trait("Component", "Carousel")]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Rejects_NonFinite_Autoplay_Interval(double autoPlayInterval) {
        Action render = () => Render<NTCarousel>(parameters => parameters
            .Add(component => component.AriaLabel, "Invalid")
            .Add(component => component.AutoPlayInterval, autoPlayInterval)
            .AddChildContent(BuildItems(("One", null))));

        render.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*finite value greater than zero*");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Rejects_Missing_Carousel_Aria_Label() {
        Action render = () => Render<NTCarousel>(parameters => parameters
            .AddChildContent(BuildItems(("One", null))));

        render.Should().Throw<InvalidOperationException>().WithMessage("*AriaLabel is required for NTCarousel*");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Rejects_Missing_Item_Aria_Label() {
        RenderFragment items = builder => {
#pragma warning disable NTC1072 // Intentionally omit the label to verify the runtime guard.
            builder.OpenComponent<NTCarouselItem>(0);
#pragma warning restore NTC1072
            builder.AddAttribute(1, nameof(NTCarouselItem.ChildContent), (RenderFragment)(content => content.AddContent(0, "Missing label")));
            builder.CloseComponent();
        };
        Action render = () => RenderCarousel(items);

        render.Should().Throw<InvalidOperationException>().WithMessage("*AriaLabel is required for NTCarouselItem*");
    }

    [Theory, Trait("Component", "Carousel")]
    [InlineData(0.5d)]
    [InlineData(2d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Rejects_Invalid_Item_Aspect_Ratio(double aspectRatio) {
        Action render = () => RenderCarousel(BuildItems(("Invalid", aspectRatio)), CarouselAppearance.UncontainedMultiAspectRatio);

        render.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*finite and between 9:16 and 16:9*");
    }

    [Fact, Trait("Component", "Carousel")]
    public void Rejects_Undefined_Appearance() {
        Action render = () => RenderCarousel(BuildItems(("One", null)), (CarouselAppearance)99);

        render.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*appearance must be a defined value*");
    }

    [Fact, Trait("Component", "Carousel")]
    public void FullScreen_Requires_Snapping() {
        Action render = () => Render<NTCarousel>(parameters => parameters
            .Add(component => component.AriaLabel, "Full screen")
            .Add(component => component.Appearance, CarouselAppearance.FullScreen)
            .Add(component => component.EnableSnapping, false)
            .AddChildContent(BuildItems(("One", null))));

        render.Should().Throw<InvalidOperationException>().WithMessage("*requires snapping*");
    }

    private RenderFragment BuildItems(params (string text, double? aspectRatio)[] items) => builder => {
        foreach (var (text, aspectRatio) in items) {
            builder.OpenComponent<NTCarouselItem>(0);
            builder.AddAttribute(1, nameof(NTCarouselItem.AriaLabel), text);
            builder.AddAttribute(2, nameof(NTCarouselItem.AspectRatio), aspectRatio);
            builder.AddAttribute(3, nameof(NTCarouselItem.ChildContent), (RenderFragment)(content => content.AddContent(0, text)));
            builder.CloseComponent();
        }
    };

    private IRenderedComponent<NTCarousel> RenderCarousel(RenderFragment items, CarouselAppearance appearance = CarouselAppearance.MultiBrowse) => Render<NTCarousel>(parameters => parameters
        .Add(component => component.AriaLabel, "Featured places")
        .Add(component => component.Appearance, appearance)
        .AddChildContent(items));
}
