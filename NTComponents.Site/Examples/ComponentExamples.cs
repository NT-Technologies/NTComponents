using Microsoft.AspNetCore.Components;
using NTComponents.Site.Examples.Buttons;
using NTComponents.Site.Examples.Charts;
using NTComponents.Site.Examples.Inputs;
using NTComponents.Site.Examples.Selection;
using NTComponents.Site.Examples.Forms;
using NTComponents.Site.Examples.Layout;
using NTComponents.Site.Examples.Overlays;
using NTComponents.Site.Examples.Display;
using NTComponents.Site.Examples.Data;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Site.Examples;

/// <summary>
///     The hand-built examples for every documented component page, keyed by component name. A component page exists only when it is listed here.
/// </summary>
public static class ComponentExamples {
    public static IReadOnlyDictionary<string, RenderFragment> Pages { get; } = new Dictionary<string, RenderFragment>(StringComparer.Ordinal) {
        ["NTButton"] = Render<ButtonExamples>(),
        ["NTButtonGroup"] = Render<ButtonGroupExamples>(),
        ["NTFabButton"] = Render<FabButtonExamples>(),
        ["NTFabMenu"] = Render<FabMenuExamples>(),
        ["NTIconButton"] = Render<IconButtonExamples>(),
        ["NTSplitButton"] = Render<SplitButtonExamples>(),
        ["NTChip"] = Render<ChipExamples>(),
        ["NTNavLink"] = Render<NavLinkExamples>(),
        ["NTInputText"] = Render<InputTextExamples>(),
        ["NTTextArea"] = Render<TextAreaExamples>(),
        ["NTInputNumeric"] = Render<InputNumericExamples>(),
        ["NTInputCurrency"] = Render<InputCurrencyExamples>(),
        ["NTColorPicker"] = Render<ColorPickerExamples>(),
        ["NTInputColor"] = Render<InputColorExamples>(),
        ["NTInputDateRange"] = Render<InputDateRangeExamples>(),
        ["NTInputDateTime"] = Render<InputDateTimeExamples>(),
        ["NTBrowserTimeZone"] = Render<BrowserTimeZoneExamples>(),
        ["NTInputCheckbox"] = Render<InputCheckboxExamples>(),
        ["NTInputSwitch"] = Render<InputSwitchExamples>(),
        ["NTSelect"] = Render<SelectExamples>(),
        ["NTInputRadioGroup"] = Render<InputRadioGroupExamples>(),
        ["NTCombobox"] = Render<ComboboxExamples>(),
        ["NTAutocomplete"] = Render<AutocompleteExamples>(),
        ["NTTypeahead"] = Render<TypeaheadExamples>(),
        ["NTInputSlider"] = Render<InputSliderExamples>(),
        ["NTInputRangeSlider"] = Render<InputRangeSliderExamples>(),
        ["NTForm"] = Render<FormExamples>(),
        ["NTFormFieldGridView"] = Render<FormFieldGridViewExamples>(),
        ["NTFormFieldLayoutSpan"] = Render<FormFieldLayoutSpanExamples>(),
        ["NTFormSectionView"] = Render<FormSectionViewExamples>(),
        ["NTFileUpload"] = Render<FileUploadExamples>(),
        ["NTRichTextEditor"] = Render<RichTextEditorExamples>(),
        ["NTWizard"] = Render<WizardExamples>(),
        ["NTLayout"] = Render<LayoutExamples>(),
        ["NTResponsive"] = Render<ResponsiveExamples>(),
        ["NTContainerView"] = Render<ContainerViewExamples>(),
        ["NTFeedView"] = Render<FeedViewExamples>(),
        ["NTListDetailView"] = Render<ListDetailViewExamples>(),
        ["NTMultiPaneView"] = Render<MultiPaneViewExamples>(),
        ["NTSupportingPaneView"] = Render<SupportingPaneViewExamples>(),
        ["NTNavigationRail"] = Render<NavigationRailExamples>(),
        ["NTTabView"] = Render<TabViewExamples>(),
        ["NTDialog"] = Render<DialogExamples>(),
        ["NTWindow"] = Render<WindowExamples>(),
        ["NTWindowHost"] = Render<WindowHostExamples>(),
        ["NTMenu"] = Render<MenuExamples>(),
        ["NTContextMenu"] = Render<ContextMenuExamples>(),
        ["NTSnackbar"] = Render<SnackbarExamples>(),
        ["NTToast"] = Render<ToastExamples>(),
        ["NTTooltip"] = Render<TooltipExamples>(),
        ["NTAccordion"] = Render<AccordionExamples>(),
        ["NTCard"] = Render<CardExamples>(),
        ["NTCarousel"] = Render<CarouselExamples>(),
        ["NTDivider"] = Render<DividerExamples>(),
        ["NTSVGIcon"] = Render<SvgIconExamples>(),
        ["NTTag"] = Render<TagExamples>(),
        ["NTShape"] = Render<ShapeExamples>(),
        ["NTSkeleton"] = Render<SkeletonExamples>(),
        ["NTLoader"] = Render<LoaderExamples>(),
        ["NTProgress"] = Render<ProgressExamples>(),
        ["NTBreadcrumb"] = Render<BreadcrumbExamples>(),
        ["NTDataGrid"] = Render<DataGridExamples>(),
        ["NTAnimation"] = Render<AnimationExamples>(),
        ["NTDebugConsole"] = Render<DebugConsoleExamples>(),
        ["NTLazyLoader"] = Render<LazyLoaderExamples>(),
        ["NTVirtualize"] = Render<VirtualizeExamples>(),
        ["NTScheduler"] = Render<SchedulerExamples>(),
        ["NTThemeToggle"] = Render<ThemeToggleExamples>(),
        ["NTHeadDependencies"] = Render<HeadDependenciesExamples>(),
        ["NTChart"] = Render<ChartExamples>(),
        ["NTLineSeries"] = Render<LineSeriesExamples>(),
        ["NTAreaSeries"] = Render<AreaSeriesExamples>(),
        ["NTBarSeries"] = Render<BarSeriesExamples>(),
        ["NTPointSeries"] = Render<PointSeriesExamples>(),
        ["NTPieSeries"] = Render<PieSeriesExamples>(),
        ["NTRadarSeries"] = Render<RadarSeriesExamples>(),
        ["NTTreeMapSeries"] = Render<TreeMapSeriesExamples>(),
        ["NTBubblePackSeries"] = Render<BubblePackSeriesExamples>(),
        ["NTHeatMapSeries"] = Render<HeatMapSeriesExamples>(),
        ["NTBoxPlotSeries"] = Render<BoxPlotSeriesExamples>()
    };

    private static RenderFragment Render<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TExamples>() where TExamples : IComponent => builder => {
        builder.OpenComponent<TExamples>(0);
        builder.CloseComponent();
    };
}
