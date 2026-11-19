using NTComponents.Virtualization;

namespace NTComponents;

/// <summary>    Represents a data request issued by <see cref="NTDataGrid{TItem}" />.</summary>
public readonly struct NTDataGridItemsProviderRequest<TItem>() where TItem : class {
    /// <summary>    Gets the zero-based index of the first requested item.</summary>
    public int StartIndex { get; init; }

    /// <summary>    Gets the maximum number of requested items, or <see langword="null" /> when the provider may return all items.</summary>
    public int? Count { get; init; }

    /// <summary>    Gets the cancellation token for the request.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>    Gets the current sort descriptors in priority order.</summary>
    public IReadOnlyList<NTSortDescriptor> Sorts { get; init; } = [];

    /// <summary>    Implicitly converts an <see cref="NTItemsProviderRequest" /> to an <see cref="NTDataGridItemsProviderRequest{TItem}" />.</summary>
    public static implicit operator NTDataGridItemsProviderRequest<TItem>(NTItemsProviderRequest request) {
        return new NTDataGridItemsProviderRequest<TItem> {
            StartIndex = request.StartIndex,
            Count = request.Count,
            Sorts = [.. request.SortOnProperties.Select(sort => new NTSortDescriptor(sort.Key, sort.Value))],
            CancellationToken = default
        };
    }

    /// <summary>    Implicitly converts an <see cref="NTDataGridItemsProviderRequest{TItem}" /> to an <see cref="NTItemsProviderRequest" />.</summary>
    public static implicit operator NTItemsProviderRequest(NTDataGridItemsProviderRequest<TItem> request) {
        return new NTItemsProviderRequest {
            StartIndex = request.StartIndex,
            Count = request.Count,
            Sorts = [.. request.Sorts.Where(sort => !string.IsNullOrWhiteSpace(sort.PropertyName)).Select(sort => $"{sort.PropertyName},{sort.Direction}")]
        };
    }
}

/// <summary>    Provides items to an <see cref="NTDataGrid{TItem}" />.</summary>
public delegate ValueTask<NTItemsProviderResult<TItem>> NTDataGridItemsProvider<TItem>(NTDataGridItemsProviderRequest<TItem> request) where TItem : class;
