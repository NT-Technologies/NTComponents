namespace NTComponents;

/// <summary>    Contains items and the total available item count returned by an NT data provider.</summary>
public readonly struct NTItemsProviderResult<TItem>() where TItem : class {
    /// <summary>    Gets the supplied items.</summary>
    public IReadOnlyCollection<TItem> Items { get; init; } = [];

    /// <summary>    Gets the total number of available items after filtering.</summary>
    public int TotalItemCount { get; init; }

    /// <summary>    Initializes a new instance of the <see cref="NTItemsProviderResult{TItem}" /> struct.</summary>
    public NTItemsProviderResult(IReadOnlyCollection<TItem> items, int totalItemCount) : this() {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}
