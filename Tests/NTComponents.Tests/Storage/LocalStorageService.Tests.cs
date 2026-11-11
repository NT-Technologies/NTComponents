using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NTComponents.Storage;

namespace NTComponents.Tests.Storage;

/// <summary>
///     Comprehensive tests for LocalStorageService functionality.
/// </summary>
public class LocalStorageService_Tests : BunitContext {
    private ILocalStorageService _localStorageService = null!;

    public LocalStorageService_Tests() {
        // Register the internal LocalStorageService through DI
        Services.AddSingleton<ILocalStorageService>(provider => {
            var jsRuntime = provider.GetRequiredService<IJSRuntime>();
            var storageServiceType = typeof(NTSkeleton).Assembly.GetType("NTComponents.Storage.LocalStorageService")!;
            return (ILocalStorageService)Activator.CreateInstance(storageServiceType, jsRuntime)!;
        });

        _localStorageService = Services.GetRequiredService<ILocalStorageService>();
    }

    [Fact]
    public async Task ClearAsync_CallsCorrectJavaScriptFunction() {
        // Arrange
        JSInterop.SetupVoid("localStorage.clear")
            .SetVoidResult();

        // Act
        await _localStorageService.ClearAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("localStorage.clear", 1);
    }

    [Fact]
    public async Task ClearAsync_WithCancellationToken_PassesToJavaScript() {
        // Arrange
        using var cts = new CancellationTokenSource();
        JSInterop.SetupVoid("localStorage.clear")
            .SetVoidResult();

        // Act
        await _localStorageService.ClearAsync(cts.Token);

        // Assert
        JSInterop.VerifyInvoke("localStorage.clear", 1);
    }

    [Fact]
    public async Task ContainKeyAsync_WithInvalidKey_ReturnsFalse() {
        // Arrange
        const string key = "non-existent-key";
        JSInterop.Setup<bool>("localStorage.hasOwnProperty", key)
            .SetResult(false);

        // Act
        var result = await _localStorageService.ContainKeyAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ContainKeyAsync_WithValidKey_ReturnsTrue() {
        // Arrange
        const string key = "test-key";
        JSInterop.Setup<bool>("localStorage.hasOwnProperty", key)
            .SetResult(true);

        // Act
        var result = await _localStorageService.ContainKeyAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        JSInterop.VerifyInvoke("localStorage.hasOwnProperty", 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetItemAsStringAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _localStorageService.GetItemAsStringAsync(invalidKey!, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task GetItemAsStringAsync_WithNonExistentKey_ReturnsNull() {
        // Arrange
        const string key = "non-existent-key";
        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult((string)null!);

        // Act
        var result = await _localStorageService.GetItemAsStringAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetItemAsStringAsync_WithValidKey_ReturnsDeserializedString() {
        // Arrange
        const string key = "string-key";
        const string expectedValue = "test-value";
        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult($"\"{expectedValue}\"");

        // Act
        var result = await _localStorageService.GetItemAsStringAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetItemAsync_WithCancellationToken_PassesToJavaScript() {
        // Arrange
        using var cts = new CancellationTokenSource();
        const string key = "cancellation-key";

        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult("\"test\"");

        // Act
        await _localStorageService.GetItemAsync<string>(key, null, cts.Token);

        // Assert
        JSInterop.VerifyInvoke("localStorage.getItem", 1);
    }

    [Fact]
    public async Task GetItemAsync_WithEmptyStringFromStorage_ReturnsDefault() {
        // Arrange
        const string key = "empty-key";
        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult("");

        // Act
        var result = await _localStorageService.GetItemAsync<string>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetItemAsync_WithInvalidJsonForString_ReturnsPlainString() {
        // Arrange
        const string key = "plain-string-key";
        const string plainStringValue = "not-json-string";

        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult(plainStringValue);

        // Act
        var result = await _localStorageService.GetItemAsync<string>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(plainStringValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetItemAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _localStorageService.GetItemAsync<string>(invalidKey!, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task GetItemAsync_WithValidIntegerValue_ReturnsDeserializedInteger() {
        // Arrange
        const string key = "int-key";
        const int expectedValue = 42;
        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult("42");

        // Act
        var result = await _localStorageService.GetItemAsync<int>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetItemAsync_WithWhitespaceFromStorage_ReturnsDefault() {
        // Arrange
        const string key = "whitespace-key";
        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult("   ");

        // Act
        var result = await _localStorageService.GetItemAsync<string>(key, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task KeyAsync_WithValidIndex_ReturnsKey() {
        // Arrange
        const int index = 0;
        const string expectedKey = "test-key";
        JSInterop.Setup<string>("localStorage.key", index)
            .SetResult(expectedKey);

        // Act
        var result = await _localStorageService.KeyAsync(index, Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedKey);
    }

    [Fact]
    public async Task KeysAsync_ReturnsAllKeys() {
        // Arrange
        var expectedKeys = new[] { "key1", "key2", "key3" };
        JSInterop.Setup<IEnumerable<string>>("eval", "Object.keys(localStorage)")
            .SetResult(expectedKeys);

        // Act
        var result = await _localStorageService.KeysAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expectedKeys);
    }

    [Fact]
    public async Task LengthAsync_ReturnsStorageLength() {
        // Arrange
        const int expectedLength = 5;
        JSInterop.Setup<int>("eval", "localStorage.length")
            .SetResult(expectedLength);

        // Act
        var result = await _localStorageService.LengthAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedLength);
    }

    [Fact]
    public void LocalStorageService_HasChangingAndChangedEvents() {
        // Act & Assert - Events should be accessible for subscription
        Action changingSubscribe = () => _localStorageService.Changing += (_, _) => { };
        Action changedSubscribe = () => _localStorageService.Changed += (_, _) => { };

        changingSubscribe.Should().NotThrow();
        changedSubscribe.Should().NotThrow();
    }

    [Fact]
    public void LocalStorageService_ImplementsILocalStorageService() {
        // Assert
        _localStorageService.Should().BeAssignableTo<ILocalStorageService>();
        _localStorageService.Should().BeAssignableTo<IStorageService>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RemoveItemAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _localStorageService.RemoveItemAsync(invalidKey!, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task RemoveItemAsync_WithValidKey_CallsJavaScriptCorrectly() {
        // Arrange
        const string key = "test-key";
        JSInterop.SetupVoid("localStorage.removeItem", key)
            .SetVoidResult();

        // Act
        await _localStorageService.RemoveItemAsync(key, Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("localStorage.removeItem", 1);
    }

    [Fact]
    public async Task RemoveItemsAsync_WithEmptyCollection_DoesNotCallJavaScript() {
        // Arrange
        var emptyKeys = Array.Empty<string>();

        // Act
        await _localStorageService.RemoveItemsAsync(emptyKeys, Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyNotInvoke("localStorage.removeItem");
    }

    [Fact]
    public async Task RemoveItemsAsync_WithMultipleKeys_CallsJavaScriptForEachKey() {
        // Arrange
        var keys = new[] { "key1", "key2", "key3" };
        foreach (var key in keys) {
            JSInterop.SetupVoid("localStorage.removeItem", key)
                .SetVoidResult();
        }

        // Act
        await _localStorageService.RemoveItemsAsync(keys, Xunit.TestContext.Current.CancellationToken);

        // Assert - Verify total number of calls to localStorage.removeItem
        JSInterop.VerifyInvoke("localStorage.removeItem", calledTimes: 3);
    }

    [Fact]
    public async Task SetItemAsStringAsync_FiresChangingAndChangedEvents() {
        // Arrange
        const string key = "event-key";
        const string newData = "new-data";
        const string oldData = "old-data";

        var changingEventArgs = (ChangingEventArgs?)null;
        ChangedEventArgs changedEventArgs = default;

        _localStorageService.Changing += (_, args) => changingEventArgs = args;
        _localStorageService.Changed += (_, args) => changedEventArgs = args;

        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult($"\"{oldData}\"");
        JSInterop.SetupVoid("localStorage.setItem", key, newData)
            .SetVoidResult();

        // Act
        await _localStorageService.SetItemAsStringAsync(key, newData, Xunit.TestContext.Current.CancellationToken);

        // Assert
        changingEventArgs.Should().NotBeNull();
        changingEventArgs!.Key.Should().Be(key);
        changingEventArgs.NewValue.Should().Be(newData);

        changedEventArgs.Key.Should().Be(key);
        changedEventArgs.NewValue.Should().Be(newData);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SetItemAsStringAsync_WithInvalidKey_ThrowsArgumentNullException(string? invalidKey) {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _localStorageService.SetItemAsStringAsync(invalidKey!, "data", cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task SetItemAsStringAsync_WithNullData_ThrowsArgumentNullException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _localStorageService.SetItemAsStringAsync("key", null!, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task SetItemAsStringAsync_WithValidData_CallsJavaScriptCorrectly() {
        // Arrange
        const string key = "test-key";
        const string data = "test-data";

        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("localStorage.setItem", key, data)
            .SetVoidResult();

        // Act
        await _localStorageService.SetItemAsStringAsync(key, data, Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("localStorage.setItem", 1);
    }

    [Fact]
    public async Task SetItemAsync_FiresChangingAndChangedEvents() {
        // Arrange
        const string key = "typed-event-key";
        const int testValue = 789;

        var changingEventArgs = (ChangingEventArgs?)null;
        ChangedEventArgs changedEventArgs = default;

        _localStorageService.Changing += (_, args) => changingEventArgs = args;
        _localStorageService.Changed += (_, args) => changedEventArgs = args;

        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("localStorage.setItem", key, "789")
            .SetVoidResult();

        // Act
        await _localStorageService.SetItemAsync(key, testValue, cancellationToken: Xunit.TestContext.Current.CancellationToken);

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
            _localStorageService.SetItemAsync(invalidKey!, 42, cancellationToken: Xunit.TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task SetItemAsync_WithNullString_SerializesNull() {
        // Arrange
        const string key = "null-string-key";
        string? nullString = null;

        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("localStorage.setItem", key, "null")
            .SetVoidResult();

        // Act
        await _localStorageService.SetItemAsync(key, nullString, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("localStorage.setItem", 1);
    }

    [Fact]
    public async Task SetItemAsync_WithPrimitiveType_SerializesAndCallsJavaScript() {
        // Arrange
        const string key = "int-key";
        const int testValue = 123;
        const string expectedJson = "123";

        JSInterop.Setup<string>("localStorage.getItem", key)
            .SetResult((string)null!);
        JSInterop.SetupVoid("localStorage.setItem", key, expectedJson)
            .SetVoidResult();

        // Act
        await _localStorageService.SetItemAsync(key, testValue, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        JSInterop.VerifyInvoke("localStorage.setItem", 1);
    }
}