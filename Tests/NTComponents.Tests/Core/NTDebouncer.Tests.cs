using NTComponents.Core;

namespace NTComponents.Tests.Core;

public class NTDebouncer_Tests {

    [Fact]
    public async Task DebounceAsync_WhenCalledAgainBeforeTheDelay_RunsOnlyTheLastAction() {
        // Arrange
        using var debouncer = new NTDebouncer(20);
        var executed = new List<int>();

        // Act
        var first = debouncer.DebounceAsync(_ => { executed.Add(1); return Task.CompletedTask; });
        var second = debouncer.DebounceAsync(_ => { executed.Add(2); return Task.CompletedTask; });
        await Task.WhenAll(first, second);

        // Assert
        executed.Should().Equal(2);
    }

}
