using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Storage;

namespace NTComponents.Tests.Storage;

/// <summary>
///     Comprehensive tests for SessionStorageService functionality.
/// </summary>
public class SessionStorageService_Tests : BunitContext {
    private ISessionStorageService _sessionStorageService = null!;

    public SessionStorageService_Tests() {
        // Register the internal SessionStorageService through DI
        Services.AddSingleton<ISessionStorageService>(provider => {
            var jsRuntime = provider.GetRequiredService<IJSRuntime>();
            var storageServiceType = typeof(NTSkeleton).Assembly.GetType("NTComponents.Storage.SessionStorageService")!;
            return (ISessionStorageService)Activator.CreateInstance(storageServiceType, jsRuntime)!;
        });

        _sessionStorageService = Services.GetRequiredService<ISessionStorageService>();
    }

    [Fact]
    public async Task ClearAsync_CallsCorrectJavaScriptFunction() {
        // Arrange
        JSInterop.SetupVoid("sessionStorage.clear")
            .SetVoidResult();

        // Act
        await _sessionStorageService.ClearAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.clear", 1);
    }

    [Fact]
    public async Task ClearAsync_WithCancellationToken_PassesToJavaScript() {
        // Arrange
        using var cts = new CancellationTokenSource();
        JSInterop.SetupVoid("sessionStorage.clear")
            .SetVoidResult();

        // Act
        await _sessionStorageService.ClearAsync(cts.Token);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.clear", 1);
    }

    [Fact]
    public async Task ContainKeyAsync_WithInvalidKey_ReturnsFalse() {
        // Arrange
        const string key = "non-existent-session-key";
        JSInterop.Setup<bool>("sessionStorage.hasOwnProperty", key)
            .SetResult(false);

        // Act
        var result = await _sessionStorageService.ContainKeyAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ContainKeyAsync_WithValidKey_ReturnsTrue() {
        // Arrange
        const string key = "session-key";
        JSInterop.Setup<bool>("sessionStorage.hasOwnProperty", key)
            .SetResult(true);

        // Act
        var result = await _sessionStorageService.ContainKeyAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        JSInterop.VerifyInvoke("sessionStorage.hasOwnProperty", 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetItemAsStringAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionStorageService.GetItemAsStringAsync(invalidKey!, Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task GetItemAsStringAsync_WithNonExistentKey_ReturnsNull() {
        // Arrange
        const string key = "non-existent-session-key";
        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult((string)null!);

        // Act
        var result = await _sessionStorageService.GetItemAsStringAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetItemAsStringAsync_WithValidKey_ReturnsDeserializedString() {
        // Arrange
        const string key = "session-string-key";
        const string expectedValue = "session-test-value";
        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult($"\"{expectedValue}\"");

        // Act
        var result = await _sessionStorageService.GetItemAsStringAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetItemAsync_WithCancellationToken_PassesToJavaScript() {
        // Arrange
        using var cts = new CancellationTokenSource();
        const string key = "session-cancellation-key";

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult("\"session-test\"");

        // Act
        await _sessionStorageService.GetItemAsync<string>(key, null, cts.Token);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.getItem", 1);
    }

    [Fact]
    public async Task GetItemAsync_WithEmptyStringFromStorage_ReturnsDefault() {
        // Arrange
        const string key = "session-empty-key";
        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult("");

        // Act
        var result = await _sessionStorageService.GetItemAsync<string>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetItemAsync_WithInvalidJsonForString_ReturnsPlainString() {
        // Arrange
        const string key = "session-plain-string-key";
        const string plainStringValue = "not-json-session-string";

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult(plainStringValue);

        // Act
        var result = await _sessionStorageService.GetItemAsync<string>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(plainStringValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetItemAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionStorageService.GetItemAsync<string>(invalidKey!, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task GetItemAsync_WithValidIntegerValue_ReturnsDeserializedInteger() {
        // Arrange
        const string key = "session-int-key";
        const int expectedValue = 42;
        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult("42");

        // Act
        var result = await _sessionStorageService.GetItemAsync<int>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetItemAsync_WithWhitespaceFromStorage_ReturnsDefault() {
        // Arrange
        const string key = "session-whitespace-key";
        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult("   ");

        // Act
        var result = await _sessionStorageService.GetItemAsync<string>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task KeyAsync_WithValidIndex_ReturnsKey() {
        // Arrange
        const int index = 0;
        const string expectedKey = "session-test-key";
        JSInterop.Setup<string>("sessionStorage.key", index)
            .SetResult(expectedKey);

        // Act
        var result = await _sessionStorageService.KeyAsync(index, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedKey);
    }

    [Fact]
    public async Task KeysAsync_ReturnsAllKeys() {
        // Arrange
        var expectedKeys = new[] { "sessionKey1", "sessionKey2", "sessionKey3" };
        JSInterop.Setup<IEnumerable<string>>("eval", "Object.keys(sessionStorage)")
            .SetResult(expectedKeys);

        // Act
        var result = await _sessionStorageService.KeysAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expectedKeys);
    }

    [Fact]
    public async Task LengthAsync_ReturnsStorageLength() {
        // Arrange
        const int expectedLength = 3;
        JSInterop.Setup<int>("eval", "sessionStorage.length")
            .SetResult(expectedLength);

        // Act
        var result = await _sessionStorageService.LengthAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedLength);
    }

    [Fact]
    public async Task RemoveItemAsync_WithCancellationToken_PassesToJavaScript() {
        // Arrange
        using var cts = new CancellationTokenSource();
        const string key = "session-remove-cancellation-key";

        JSInterop.SetupVoid("sessionStorage.removeItem", key)
            .SetVoidResult();

        // Act
        await _sessionStorageService.RemoveItemAsync(key, cts.Token);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.removeItem", 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RemoveItemAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionStorageService.RemoveItemAsync(invalidKey!, Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task RemoveItemAsync_WithValidKey_CallsJavaScriptCorrectly() {
        // Arrange
        const string key = "session-remove-key";
        JSInterop.SetupVoid("sessionStorage.removeItem", key)
            .SetVoidResult();

        // Act
        await _sessionStorageService.RemoveItemAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.removeItem", 1);
    }

    [Fact]
    public async Task RemoveItemsAsync_WithEmptyCollection_DoesNotCallJavaScript() {
        // Arrange
        var emptyKeys = Array.Empty<string>();

        // Act
        await _sessionStorageService.RemoveItemsAsync(emptyKeys, Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyNotInvoke("sessionStorage.removeItem");
    }

    [Fact]
    public async Task RemoveItemsAsync_WithMultipleKeys_CallsJavaScriptForEachKey() {
        // Arrange
        var keys = new[] { "sessionKey1", "sessionKey2", "sessionKey3" };
        foreach (var key in keys) {
            JSInterop.SetupVoid("sessionStorage.removeItem", key)
                .SetVoidResult();
        }

        // Act
        await _sessionStorageService.RemoveItemsAsync(keys, Xunit.TestContext.Current.CancellationToken);

        // Assert - Verify total number of calls to sessionStorage.removeItem
        JSInterop.VerifyInvoke("sessionStorage.removeItem", calledTimes: 3);
    }

    [Fact]
    public void SessionStorageService_HasChangingAndChangedEvents() {
        // Act & Assert - Events should be accessible for subscription
        Action changingSubscribe = () => _sessionStorageService.Changing += (_, _) => { };
        Action changedSubscribe = () => _sessionStorageService.Changed += (_, _) => { };

        changingSubscribe.Should().NotThrow();
        changedSubscribe.Should().NotThrow();
    }

    [Fact]
    public void SessionStorageService_ImplementsISessionStorageService() {
        // Assert
        _sessionStorageService.Should().BeAssignableTo<ISessionStorageService>();
        _sessionStorageService.Should().BeAssignableTo<IStorageService>();
    }

    [Fact]
    public async Task SetItemAsStringAsync_FiresChangingAndChangedEvents() {
        // Arrange
        const string key = "session-event-key";
        const string newData = "session-new-data";
        const string oldData = "session-old-data";

        var changingEventArgs = (ChangingEventArgs?)null;
        ChangedEventArgs changedEventArgs = default;

        _sessionStorageService.Changing += (_, args) => changingEventArgs = args;
        _sessionStorageService.Changed += (_, args) => changedEventArgs = args;

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult($"\"{oldData}\"");
        JSInterop.SetupVoid("sessionStorage.setItem", key, newData)
            .SetVoidResult();

        // Act
        await _sessionStorageService.SetItemAsStringAsync(key, newData, Xunit.TestContext.Current.CancellationToken);

        // Assert
        changingEventArgs.Should().NotBeNull();
        changingEventArgs!.Key.Should().Be(key);
        changingEventArgs.NewValue.Should().Be(newData);

        changedEventArgs.Key.Should().Be(key);
        changedEventArgs.NewValue.Should().Be(newData);
    }

    [Fact]
    public async Task SetItemAsStringAsync_WithCancellationToken_PassesToJavaScript() {
        // Arrange
        using var cts = new CancellationTokenSource();
        const string key = "session-set-cancellation-key";
        const string data = "session-cancellation-data";

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("sessionStorage.setItem", key, data)
            .SetVoidResult();

        // Act
        await _sessionStorageService.SetItemAsStringAsync(key, data, cts.Token);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.setItem", 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SetItemAsStringAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionStorageService.SetItemAsStringAsync(invalidKey!, "data", Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task SetItemAsStringAsync_WithNullData_ThrowsArgumentNullException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionStorageService.SetItemAsStringAsync("key", null!, Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task SetItemAsStringAsync_WithValidData_CallsJavaScriptCorrectly() {
        // Arrange
        const string key = "session-test-key";
        const string data = "session-test-data";

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("sessionStorage.setItem", key, data)
            .SetVoidResult();

        // Act
        await _sessionStorageService.SetItemAsStringAsync(key, data, Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.setItem", 1);
    }

    [Fact]
    public async Task SetItemAsync_FiresChangingAndChangedEvents() {
        // Arrange
        const string key = "session-typed-event-key";
        const int testValue = 999;

        var changingEventArgs = (ChangingEventArgs?)null;
        ChangedEventArgs changedEventArgs = default;

        _sessionStorageService.Changing += (_, args) => changingEventArgs = args;
        _sessionStorageService.Changed += (_, args) => changedEventArgs = args;

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("sessionStorage.setItem", key, "999")
            .SetVoidResult();

        // Act
        await _sessionStorageService.SetItemAsync(key, testValue, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        changingEventArgs.Should().NotBeNull();
        changingEventArgs!.Key.Should().Be(key);
        changingEventArgs.NewValue.Should().Be(testValue);

        changedEventArgs.Key.Should().Be(key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SetItemAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sessionStorageService.SetItemAsync(invalidKey!, 42, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task SetItemAsync_WithNullString_SerializesNull() {
        // Arrange
        const string key = "session-null-string-key";
        string? nullString = null;

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("sessionStorage.setItem", key, "null")
            .SetVoidResult();

        // Act
        await _sessionStorageService.SetItemAsync(key, nullString, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.setItem", 1);
    }

    [Fact]
    public async Task SetItemAsync_WithPrimitiveType_SerializesAndCallsJavaScript() {
        // Arrange
        const string key = "session-int-key";
        const int testValue = 123;
        const string expectedJson = "123";

        JSInterop.Setup<string>("sessionStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("sessionStorage.setItem", key, expectedJson)
            .SetVoidResult();

        // Act
        await _sessionStorageService.SetItemAsync(key, testValue, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("sessionStorage.setItem", 1);
    }
}