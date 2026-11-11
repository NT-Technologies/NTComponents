using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Core;

/// <summary>
///     Generates unique identifiers for components in the NTComponents library.
/// </summary>
[ExcludeFromCodeCoverage]
public static class NTComponentIdentifier {
    private const string _identifierPrefix = "tnt_";

    /// <summary>
    ///     Returns a new small Id. HTML id must start with a letter.
    ///     Example: f127d9edf14385adb
    /// </summary>
    /// <remarks>You can use a <see cref="NTComponentIdentifierContext" /> instance to customize the Generation process, for example in Unit Tests.</remarks>
    /// <returns>A new id</returns>
    public static string NewId(int length = 8) {
#pragma warning disable IDE0046 // Convert to conditional expression
        if (NTComponentIdentifierContext.Current == null) {
            if (length > 16) {
                throw new ArgumentOutOfRangeException(nameof(length), "length must be less than 16");
            }
            if (length <= 8) {
                return $"{_identifierPrefix}{Random.Shared.Next():x}";
            }

            return $"{_identifierPrefix}{Random.Shared.Next():x}{Random.Shared.Next():x}"[..length];
        }

        return NTComponentIdentifierContext.Current.GenerateId();
#pragma warning restore IDE0046 // Convert to conditional expression
    }
}

/// <summary>
///     Provides a context for generating unique identifiers for components in the NTComponents library. ///
/// </summary>
[ExcludeFromCodeCoverage]
public class NTComponentIdentifierContext : IDisposable {

    /// <summary>
    ///     The current context for generating unique identifiers.
    /// </summary>
    public static NTComponentIdentifierContext? Current => _threadScopeStack.Value == null || _threadScopeStack.Value.Count == 0 ? null : (_threadScopeStack.Value?.Peek());

    private uint _currentIndex { get; set; }
    private Func<uint, string> _newId { get; init; }
    private static readonly ThreadLocal<Stack<NTComponentIdentifierContext>> _threadScopeStack = new(() => new Stack<NTComponentIdentifierContext>());

    /// <summary>
    ///     Constructor for NTComponentIdentifierContext.
    /// </summary>
    /// <param name="newId">A function for generating a new id</param>
    public NTComponentIdentifierContext(Func<uint, string> newId) {
        _threadScopeStack.Value?.Push(this);
        _newId = newId;
        _currentIndex = 0;
    }

    /// <inheritdoc />
    public void Dispose() {
        GC.SuppressFinalize(this);
        _ = _threadScopeStack.Value?.TryPop(out _);
    }

    internal string GenerateId() {
        var id = _newId.Invoke(_currentIndex);

        _currentIndex++;

        if (_currentIndex >= 99999999) {
            _currentIndex = 0;
        }

        return id;
    }
}