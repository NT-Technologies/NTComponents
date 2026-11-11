using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using NTComponents.Core;
using NTComponents.Ext;

using NTComponents.CodeDocumentation;
namespace NTComponents;

/// <summary>
///     Material 3 linear and ring progress indicator.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="NTProgress" /> for determinate or indeterminate process progress. Use <see cref="NTLoader" /> for the Material 3 shape loading indicator.
///     </para>
///     <para>
///         The component is static SSR-friendly. It renders plain HTML, SVG, ARIA attributes, and CSS variables from parameters, and it does not require JavaScript interop or an interactive Blazor render
///         mode for its initial visual state or CSS-only indeterminate animation.
///     </para>
///     <para>
///         Use <see cref="Value" /> with a positive <see cref="Max" /> for determinate progress. Leave <see cref="Value" /> as <see langword="null" /> for indeterminate progress. If progress changes after
///         the initial static SSR response, the parent page must provide a new render or update the emitted CSS custom properties from JavaScript.
///     </para>
///     <para>
///         Use <see cref="TrackVisible" /> sparingly. It is intended for embedded circular indicators, such as progress inside buttons, where the surrounding component supplies enough contrast.
///     </para>
/// </remarks>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.SsrCompatible,
    CompatibilitySummary = "Renders progress markup and CSS-only animation without JavaScript.",
    CompatibilityDetails = "Static SSR emits the progress role, ARIA values, SVG or track markup, and CSS variables. Later progress changes require a new render or external script updates.")]
public partial class NTProgress : NTDisposableComponentBase {

    /// <summary>
    ///     Whether CSS progress motion is active.
    /// </summary>
    /// <remarks>
    ///     This toggles CSS animation classes only. It does not require an interactive Blazor render mode once the component has rendered.
    /// </remarks>
    [Parameter]
    public bool Animate { get; set; } = true;

    /// <summary>
    ///     Accessible label used when no <c>aria-label</c> or <c>aria-labelledby</c> attribute is provided.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; } = "Loading";

    /// <summary>
    ///     Optional id of visible text that labels the progress indicator.
    /// </summary>
    [Parameter]
    public string? AriaLabelledBy { get; set; }

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("nt-progress")
        .AddClass(IsLinearVariant ? "nt-progress-linear" : "nt-progress-ring")
        .AddClass("nt-progress-sine-wave", IsSineWaveAppearance)
        .AddClass("nt-progress-determinate", IsDeterminate)
        .AddClass("nt-progress-indeterminate", IsIndeterminate)
        .AddClass("nt-progress-started", IsDeterminate && _effectiveProgressValue > 0)
        .AddClass("nt-progress-complete", IsDeterminate && _effectiveProgressValue >= _effectiveMax)
        .AddClass("nt-progress-track-hidden", !TrackVisible)
        .AddClass("nt-progress-animated", Animate)
        .AddSize(Size)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddVariable("nt-progress-indicator-color", ProgressColor.ToCssNTColorVariable())
        .AddVariable("nt-progress-track-color", TrackColor.ToCssNTColorVariable())
        .AddVariable("nt-progress-active-angle", _effectiveProgressAngle, IsDeterminate)
        .AddVariable("nt-progress-active-value", _effectiveProgressPercentValue, IsDeterminate)
        .AddVariable("nt-progress-active-percentage", _effectiveProgressPercentage, IsDeterminate)
        .Build();

    /// <summary>
    ///     Maximum progress value for determinate progress indicators.
    /// </summary>
    /// <remarks>
    ///     Use a positive value. Non-positive values fall back to <c>100</c> so the component can render defensively, but callers should pass the real maximum to keep ARIA and visual progress truthful.
    /// </remarks>
    [Parameter]
    public double Max { get; set; } = 100.0;

    /// <summary>
    ///     Color used by the active progress indicator.
    /// </summary>
    [Parameter]
    public NTColor ProgressColor { get; set; } = NTColor.Primary;

    /// <summary>
    ///     Whether to render the progress indicator.
    /// </summary>
    [Parameter]
    public bool Show { get; set; } = true;

    /// <summary>
    ///     Token size for the progress indicator.
    /// </summary>
    [Parameter]
    public NTSize Size { get; set; } = NTSize.Medium;

    /// <summary>
    ///     Color used by the inactive track.
    /// </summary>
    [Parameter]
    public NTColor TrackColor { get; set; } = NTColor.SecondaryContainer;

    /// <summary>
    ///     Whether to show the inactive track. Set to <see langword="false" /> when placing a ring progress indicator inside a button.
    /// </summary>
    [Parameter]
    public bool TrackVisible { get; set; } = true;

    /// <summary>
    ///     Progress indicator layout and active-track appearance variant.
    /// </summary>
    [Parameter]
    public NTProgressVariant Variant { get; set; } = NTProgressVariant.Linear;

    /// <summary>
    ///     Current progress value. Set to <see langword="null" /> for indeterminate progress.
    /// </summary>
    /// <remarks>
    ///     Determinate values are clamped to the range from <c>0</c> through <see cref="Max" /> for rendering. Prefer passing an already bounded value so the visual indicator and process state stay in
    ///     sync.
    /// </remarks>
    [Parameter]
    public double? Value { get; set; }

    private string? EffectiveAriaLabel => GetAdditionalAttributeValue("aria-label") ?? (!string.IsNullOrWhiteSpace(EffectiveAriaLabelledBy) ? null : AriaLabel);
    private string? EffectiveAriaLabelledBy => GetAdditionalAttributeValue("aria-labelledby") ?? AriaLabelledBy;
    private string? EffectiveAriaValueMax => GetAdditionalAttributeValue("aria-valuemax") ?? (IsDeterminate ? _effectiveMax.ToString(CultureInfo.InvariantCulture) : null);
    private string? EffectiveAriaValueMin => GetAdditionalAttributeValue("aria-valuemin") ?? (IsDeterminate ? "0" : null);
    private string? EffectiveAriaValueNow => GetAdditionalAttributeValue("aria-valuenow") ?? (IsDeterminate ? _effectiveProgressValue.ToString(CultureInfo.InvariantCulture) : null);
    private string EffectiveRole => GetAdditionalAttributeValue("role") ?? "progressbar";
    private bool IsDeterminate => Value.HasValue;
    private bool IsIndeterminate => !Value.HasValue;
    private bool IsLinearVariant => Variant is NTProgressVariant.Linear or NTProgressVariant.LinearWave;
    private bool IsSineWaveAppearance => Variant is NTProgressVariant.LinearWave or NTProgressVariant.RingWave;
    private bool ShouldRenderStop => IsLinearVariant && IsDeterminate && TrackVisible;
    private double _effectiveMax = 100.0;
    private double _effectiveProgressValue;
    private string _effectiveProgressAngle = "0deg";
    private string _effectiveProgressPercentage = "0%";
    private string _effectiveProgressPercentValue = "0";
    private const int LinearSineWavePathWidth = 4080;
    private const double LinearSineWaveAmplitude = 3.0;
    private static string? _linearSineWavePathData10;
    private static string? _linearSineWavePathData12;
    private static string? _linearSineWavePathData14;
    private const double RingSineWaveContainerSize = 48.0;
    private const double RingSineWaveStrokeWidth = 4.0;
    private const double RingSineWaveAmplitude = 1.6;
    private const double RingSineWaveWavelength = 15.0;
    private const int RingSineWaveSamplesPerWave = 16;
    private const int RingSineWaveAnimationFrameCount = 8;
    private const double RingSineWaveViewBoxScale = 100.0 / RingSineWaveContainerSize;
    private const double RingSineWaveOuterPathRadius = ((RingSineWaveContainerSize - RingSineWaveStrokeWidth) / 2.0) * RingSineWaveViewBoxScale;
    private const double RingSineWaveBaseRadius = RingSineWaveOuterPathRadius - (RingSineWaveAmplitude * RingSineWaveViewBoxScale);
    private static RingSineWavePathFrame[]? _ringSineWavePathFrames;
    private static RingSineWavePathFrame[] RingSineWavePathFrames => _ringSineWavePathFrames ??= BuildRingSineWavePathFrames();
    private double EffectiveLinearSineWaveHeight => EffectiveLinearThickness + (LinearSineWaveAmplitude * 2.0);
    private double EffectiveLinearThickness
        => Size switch {
            NTSize.Large => 6.0,
            NTSize.Largest => 8.0,
            _ => 4.0
        };

    private string LinearSineWavePathData
        => EffectiveLinearSineWaveHeight switch {
            12.0 => _linearSineWavePathData12 ??= BuildLinearSineWavePathData(12.0),
            14.0 => _linearSineWavePathData14 ??= BuildLinearSineWavePathData(14.0),
            _ => _linearSineWavePathData10 ??= BuildLinearSineWavePathData(10.0)
        };

    private string LinearSineWaveViewBox => $"0 0 {LinearSineWavePathWidth} {LinearSineWaveViewBoxHeight}";
    private string LinearSineWaveViewBoxHeight => FormatCssNumber(EffectiveLinearSineWaveHeight);
    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();

        _effectiveMax = Max > 0 ? Max : 100.0;
        if (!IsDeterminate) {
            // Indeterminate indicators never emit these values, so skip formatting them.
            _effectiveProgressValue = 0;
            _effectiveProgressAngle = "0deg";
            _effectiveProgressPercentage = "0%";
            _effectiveProgressPercentValue = "0";
            return;
        }

        _effectiveProgressValue = Math.Clamp(Value.GetValueOrDefault(), 0, _effectiveMax);
        _effectiveProgressPercentValue = FormatCssNumber(_effectiveProgressValue / _effectiveMax * 100.0);
        _effectiveProgressPercentage = $"{_effectiveProgressPercentValue}%";
        _effectiveProgressAngle = $"{FormatCssNumber(_effectiveProgressValue / _effectiveMax * 360.0)}deg";
    }

    private string? GetAdditionalAttributeValue(string name)
        => AdditionalAttributes?.TryGetValue(name, out var value) == true && value is not null
            ? value.ToString()
            : null;

    private static string FormatCssNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string BuildLinearSineWavePathData(double height) {
        var center = height / 2.0;
        var crest = center - LinearSineWaveAmplitude;
        var trough = center + LinearSineWaveAmplitude;
        var builder = new StringBuilder($"M -40 {FormatCssNumber(center)}");

        for (var x = -40; x < LinearSineWavePathWidth; x += 40) {
            builder
                .Append(CultureInfo.InvariantCulture, $" C {x + 5} {FormatCssNumber(crest)} {x + 15} {FormatCssNumber(crest)} {x + 20} {FormatCssNumber(center)}")
                .Append(CultureInfo.InvariantCulture, $" C {x + 25} {FormatCssNumber(trough)} {x + 35} {FormatCssNumber(trough)} {x + 40} {FormatCssNumber(center)}");
        }

        return builder.ToString();
    }

    private static string BuildRingSineWavePathData(double phase = 0.0) {
        var amplitude = RingSineWaveAmplitude * RingSineWaveViewBoxScale;
        var baseRadius = RingSineWaveBaseRadius;
        var waveCount = Math.Max(5, (int)(2.0 * Math.PI * ((RingSineWaveContainerSize - RingSineWaveStrokeWidth) / 2.0) / RingSineWaveWavelength));
        var sampleCount = waveCount * RingSineWaveSamplesPerWave;
        var builder = new StringBuilder();

        for (var i = 0; i <= sampleCount; i++) {
            AppendRingSineWavePoint(builder, i, i == 0, sampleCount, waveCount, baseRadius, amplitude, phase);
        }

        return builder.ToString();
    }

    private static RingSineWavePathFrame[] BuildRingSineWavePathFrames() {
        var frames = new RingSineWavePathFrame[RingSineWaveAnimationFrameCount];

        for (var frame = 0; frame < frames.Length; frame++) {
            var phase = -2.0 * Math.PI * frame / RingSineWaveAnimationFrameCount;
            frames[frame] = new RingSineWavePathFrame(BuildRingSineWavePathData(phase), $"--nt-progress-ring-wave-frame-index:{frame}");
        }

        return frames;
    }

    private static void AppendRingSineWavePoint(StringBuilder builder, double sample, bool isFirst, int sampleCount, int waveCount, double baseRadius, double amplitude, double phase) {
        var angle = 2.0 * Math.PI * sample / sampleCount;
        var radius = baseRadius + (amplitude * Math.Sin((waveCount * angle) + phase));
        var x = 50.0 + (Math.Cos(angle) * radius);
        var y = 50.0 + (Math.Sin(angle) * radius);
        builder.Append(isFirst ? "M " : " L ");
        builder
            .Append(FormatCssNumber(x))
            .Append(' ')
            .Append(FormatCssNumber(y));
    }

    private sealed record RingSineWavePathFrame(string PathData, string Style);
}

/// <summary>
///     Progress indicator visual variants.
/// </summary>
public enum NTProgressVariant {

    /// <summary>
    ///     Horizontal linear progress indicator.
    /// </summary>
    Linear,

    /// <summary>
    ///     Horizontal linear progress indicator with a sine wave active indicator.
    /// </summary>
    LinearWave,

    /// <summary>
    ///     Circular ring progress indicator.
    /// </summary>
    Ring,

    /// <summary>
    ///     Circular ring progress indicator with a sine wave active indicator.
    /// </summary>
    RingWave
}
