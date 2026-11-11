using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Core;
using NTComponents.Toast;

namespace NTComponents.Tests.Toast;

/// <summary>
///     Unit tests for <see cref="NTToastService" />.
/// </summary>
public class NTToastService_Tests : BunitContext {
    private bool _closeShouldFail;
    private IReadOnlyList<object?> _queuedArguments = [];

    public NTToastService_Tests() {
        var module = JSInterop.SetupModule(NTToast.JsModulePathValue);
        module.Setup<string>("queueToastFromBlazor", invocation => {
            _queuedArguments = invocation.Arguments.ToArray();
            return true;
        }).SetResult("queued");
        module.SetupVoid("clearToastsFromBlazor", _ => true).SetVoidResult();
        module.Setup<bool>("closeToastFromBlazor", _ => !_closeShouldFail).SetResult(true);
        module.Setup<bool>("closeToastFromBlazor", _ => _closeShouldFail).SetResult(false);
    }

    [Fact]
    public void Constructor_InitializesCorrectly() {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<INTToastService>();
        service.ActiveToasts.Should().BeEmpty();
    }

    [Fact]
    public void AddNTServices_RegistersToastService() {
        // Arrange & Act
        Services.AddNTServices();

        // Assert
        Services.GetRequiredService<INTToastService>().Should().BeAssignableTo<INTToastService>();
    }

    [Fact]
    public async Task ShowAsync_QueuesToastThroughJavaScript_WithMaterialDefaults() {
        // Arrange
        var service = CreateService();
        INTToast? openedToast = null;
        service.OnOpen += toast => {
            openedToast = toast;
            return Task.CompletedTask;
        };

        // Act
        await service.ShowAsync("Saved", "Changes were stored.");

        // Assert
        openedToast.Should().NotBeNull();
        openedToast!.Title.Should().Be("Saved");
        openedToast.Message.Should().Be("Changes were stored.");
        openedToast.Timeout.Should().Be(4);
        openedToast.ShowClose.Should().BeTrue();
        openedToast.BackgroundColor.Should().Be(NTColor.SurfaceContainerHigh);
        openedToast.TextColor.Should().Be(NTColor.OnSurface);
        openedToast.IconColor.Should().Be(NTColor.Primary);
        openedToast.Variant.Should().Be(NTToastVariant.Default);

        GetQueuedArgument<string>(1).Should().Be("Saved");
        GetQueuedArgument<string>(2).Should().Be("Changes were stored.");
        GetQueuedArgument<double>(4).Should().Be(4);
        GetQueuedArgument<bool>(5).Should().BeTrue();
        GetQueuedArgument<string>(7).Should().BeNull();
        GetQueuedArgument<string>(8).Should().BeNull();
        GetQueuedArgument<string>(9).Should().BeNull();
        GetQueuedArgument<string>(3).Should().Be("default");
        GetQueuedArgument<object>(10).Should().NotBeNull();
        GetQueuedArgument<string>(11).Should().Be(nameof(NTToastService.NotifyClosedFromJavaScript));
        JSInterop.VerifyInvoke("import", 1);
        JSInterop.VerifyInvoke("queueToastFromBlazor", 1);
        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "queueToast");
    }

    [Fact]
    public async Task ShowAsync_HonorsExplicitOverrides() {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowAsync("Custom", "Overrides", NTToastVariant.Warning, timeout: 9, showClose: false, icon: "star", backgroundColor: NTColor.Primary, textColor: NTColor.OnPrimary, iconColor: NTColor.Secondary);

        // Assert
        GetQueuedArgument<double>(4).Should().Be(9);
        GetQueuedArgument<bool>(5).Should().BeFalse();
        GetQueuedArgument<string>(6).Should().Be("star");
        GetQueuedArgument<string>(7).Should().Be("var(--nt-color-primary)");
        GetQueuedArgument<string>(8).Should().Be("var(--nt-color-on-primary)");
        GetQueuedArgument<string>(9).Should().Be("var(--nt-color-secondary)");
        GetQueuedArgument<string>(3).Should().Be("warning");
    }

    [Theory]
    [InlineData(NTToastVariant.Success, "var(--nt-color-success-container)", "var(--nt-color-on-success-container)", "var(--nt-color-success)")]
    [InlineData(NTToastVariant.Info, "var(--nt-color-info-container)", "var(--nt-color-on-info-container)", "var(--nt-color-info)")]
    [InlineData(NTToastVariant.Warning, "var(--nt-color-warning-container)", "var(--nt-color-on-warning-container)", "var(--nt-color-warning)")]
    [InlineData(NTToastVariant.Error, "var(--nt-color-error-container)", "var(--nt-color-on-error-container)", "var(--nt-color-error)")]
    [InlineData(NTToastVariant.Assert, "var(--nt-color-assert-container)", "var(--nt-color-on-assert-container)", "var(--nt-color-assert)")]
    public async Task ShowAsync_UsesVariantColors(NTToastVariant variant, string backgroundColor, string textColor, string iconColor) {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowAsync("Variant", variant: variant);

        // Assert
        service.ActiveToasts[^1].BackgroundColor.ToCssNTColorVariable().Should().Be(backgroundColor);
        service.ActiveToasts[^1].TextColor.ToCssNTColorVariable().Should().Be(textColor);
        service.ActiveToasts[^1].IconColor.ToCssNTColorVariable().Should().Be(iconColor);
        GetQueuedArgument<string>(7).Should().BeNull();
        GetQueuedArgument<string>(8).Should().BeNull();
        GetQueuedArgument<string>(9).Should().BeNull();
        GetQueuedArgument<string>(3).Should().Be(variant.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task ExtensionMethods_ShowSemanticVariants() {
        // Arrange
        var service = CreateService();

        // Act
        await service.ShowSuccessAsync("Success");
        await service.ShowInfoAsync("Info");
        await service.ShowWarningAsync("Warning");
        await service.ShowErrorAsync("Error");
        await service.ShowAssertAsync("Assert");

        // Assert
        service.ActiveToasts.Select(toast => toast.Variant).Should().ContainInOrder(
            NTToastVariant.Success,
            NTToastVariant.Info,
            NTToastVariant.Warning,
            NTToastVariant.Error,
            NTToastVariant.Assert);
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "queueToastFromBlazor").Should().Be(5);
    }

    [Fact]
    public async Task CloseAsync_ClosesToastThroughJavaScript_AndRaisesClose() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Saved");

        // Act
        await service.CloseAsync(service.ActiveToasts[0]);

        // Assert
        closeCount.Should().Be(1);
        service.ActiveToasts.Should().BeEmpty();
        JSInterop.VerifyInvoke("closeToastFromBlazor", 1);
    }

    [Fact]
    public async Task CloseAsync_WhenJavaScriptDoesNotClose_KeepsToastTracked() {
        // Arrange
        var service = CreateService();
        await service.ShowAsync("Saved");
        _closeShouldFail = true;

        // Act
        await service.CloseAsync(service.ActiveToasts[0]);

        // Assert
        service.ActiveToasts.Should().ContainSingle();
        JSInterop.VerifyInvoke("closeToastFromBlazor", 1);
    }

    [Fact]
    public async Task NotifyClosedFromJavaScript_RemovesToast_AndRaisesClose() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };

        await service.ShowAsync("Saved");
        var toastId = ((NTToastService.NTToastImplementation)service.ActiveToasts[0]).Id;

        // Act
        await service.NotifyClosedFromJavaScript(toastId);

        // Assert
        closeCount.Should().Be(1);
        service.ActiveToasts.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowAsync_WhenJavaScriptInteropIsUnavailable_ThrowsActionableExceptionAndUntracksToast() {
        // Arrange
        var service = new NTToastService(new UnavailableJavaScriptRuntime());

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ShowAsync("Prerender"));

        // Assert
        exception.Message.Should().Contain("interactive Blazor render context");
        exception.Message.Should().Contain("window.NTToast?.queueToast");
        service.ActiveToasts.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposeAsync_ClearsTrackedJavaScriptToasts_WithoutCloseCallbacks() {
        // Arrange
        var service = CreateService();
        var closeCount = 0;
        service.OnClose += _ => {
            closeCount++;
            return Task.CompletedTask;
        };
        await service.ShowAsync("Saved");

        // Act
        await service.DisposeAsync();

        // Assert
        closeCount.Should().Be(0);
        service.ActiveToasts.Should().BeEmpty();
        JSInterop.VerifyInvoke("clearToastsFromBlazor", 1);
    }

    private NTToastService CreateService() {
        return new NTToastService(Services.GetRequiredService<IJSRuntime>());
    }

    private T? GetQueuedArgument<T>(int index) {
        _queuedArguments.Should().HaveCountGreaterThan(index);
        return (T?)_queuedArguments[index];
    }

    private sealed class UnavailableJavaScriptRuntime : IJSRuntime {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) {
            throw new InvalidOperationException("JavaScript interop calls cannot be issued during server-side static rendering.");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
            throw new InvalidOperationException("JavaScript interop calls cannot be issued during server-side static rendering.");
        }
    }
}
