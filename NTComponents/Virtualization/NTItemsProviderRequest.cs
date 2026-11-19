using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NTComponents.Virtualization;

/// <summary>
///     Represents an NT item request that can be bound from HTTP query parameters.
/// </summary>
/// <remarks>
///     This endpoint accepts query parameters in the form: <c>?StartIndex=0&amp;Count=10&amp;Sorts=Name,Ascending&amp;Sorts=Age,Descending</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record NTItemsProviderRequest : IValidatableObject {
    private readonly int? _startIndex;

    /// <summary>
    ///     Gets the start index of the requested items.
    /// </summary>
    public int StartIndex {
        get => _startIndex.GetValueOrDefault();
        init => _startIndex = value;
    }

    /// <summary>
    ///     Gets the maximum number of items to retrieve.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    ///     Gets the sort descriptors formatted as <c>PropertyName,Direction</c>.
    /// </summary>
    public string[] Sorts { get; init; } = [];

    /// <summary>
    ///     Gets the parsed properties to sort on and their sort directions.
    /// </summary>
    [IgnoreDataMember]
    [JsonIgnore]
    public IReadOnlyList<KeyValuePair<string, SortDirection>> SortOnProperties => [.. Sorts?.Where(sort => !string.IsNullOrWhiteSpace(sort)).SelectMany(sort => ParseSort(sort.Replace("[", string.Empty, StringComparison.Ordinal).Replace("]", string.Empty, StringComparison.Ordinal))) ?? []];

    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext) {
        if (!_startIndex.HasValue) {
            yield return new ValidationResult("StartIndex is required.", [nameof(StartIndex)]);
        }
    }

    /// <summary>
    ///     Binds the HTTP context query parameters to an <see cref="NTItemsProviderRequest" /> instance.
    /// </summary>
    public static ValueTask<NTItemsProviderRequest?> BindAsync(HttpContext context) {
        var query = context.Request.Query;
        if (!int.TryParse(query[nameof(StartIndex)].FirstOrDefault(), out var startIndex)) {
            return ValueTask.FromResult((NTItemsProviderRequest?)null);
        }

        var sorts = GetQueryValues(query, nameof(Sorts));
        return ValueTask.FromResult<NTItemsProviderRequest?>(new NTItemsProviderRequest {
            StartIndex = startIndex,
            Count = int.TryParse(query[nameof(Count)].FirstOrDefault(), out var count) ? count : null,
            Sorts = sorts.Length == 0 ? GetQueryValues(query, nameof(SortOnProperties)) : sorts
        });
    }

    internal static string[] FormatSorts(IEnumerable<KeyValuePair<string, SortDirection>>? sortOnProperties) =>
        [.. sortOnProperties?.Where(sort => !string.IsNullOrWhiteSpace(sort.Key)).Select(sort => $"{sort.Key},{sort.Value}") ?? []];

    private static IEnumerable<KeyValuePair<string, SortDirection>> ParseSort(string sort) {
        var sortParts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < sortParts.Length; i += 2) {
            yield return new(sortParts[i], i + 1 < sortParts.Length && Enum.TryParse<SortDirection>(sortParts[i + 1], true, out var direction) ? direction : SortDirection.Ascending);
        }
    }

    private static string[] GetQueryValues(IQueryCollection query, string key) =>
        query.TryGetValue(key, out var values) ? [.. values.OfType<string>().Where(value => !string.IsNullOrWhiteSpace(value))] : [];
}
