using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using NTComponents.CodeDocumentation;
using NTComponents.Ext;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
namespace NTComponents;

/// <summary>    Renders a column bound to a property expression.</summary>
[NTDocumentation(
    RenderCompatibility = NTComponentRenderCompatibility.ProgressivelyEnhanced,
    CompatibilitySummary = "Participates in parent component rendering and inherits the parent interaction model.",
    CompatibilityDetails = "The column can contribute header and cell content to the parent grid during static SSR. Sorting and grid state changes depend on the parent grid render mode.")]
public sealed class NTPropertyColumn<TItem, TValue> : NTDataGridColumn<TItem> where TItem : class {
    private Func<TItem, TValue>? _accessor;
    private string? _accessorExpressionText;
    private MemberInfo? _accessorMember;
    private Expression<Func<TItem, TValue>>? _propertyMetadataSource;
    private string _defaultTitle = string.Empty;
    private string _memberName = string.Empty;

    /// <summary>    Gets or sets whether the column can be sorted.</summary>
    public override bool Sortable => true;

    /// <summary>    Gets or sets the property expression rendered by the column.</summary>
    [Parameter]
    [EditorRequired]
    public Expression<Func<TItem, TValue>>? Property { get; set; }

    /// <summary>    Gets or sets the format string used for values implementing <see cref="IFormattable" />.</summary>
    [Parameter]
    public string? Format { get; set; }

    internal override string DefaultTitle => _defaultTitle;

    internal override string? SortPropertyName => _memberName;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (!ReferenceEquals(_propertyMetadataSource, Property)) {
            _propertyMetadataSource = Property;
            var memberName = NTDataGridColumn<TItem>.GetMemberName(Property);
            if (!string.Equals(_memberName, memberName, StringComparison.Ordinal)) {
                _memberName = memberName;
                _defaultTitle = memberName.SplitPascalCase();
            }
        }

        // A fresh expression instance usually arrives on every parent render, so skip printing it when it is the same plain member access.
        var member = Property?.Body is MemberExpression { Expression: ParameterExpression } memberExpression ? memberExpression.Member : null;
        if (_accessor is null || member is null || !ReferenceEquals(member, _accessorMember)) {
            var expressionText = Property?.ToString();
            if (_accessor is null || !string.Equals(_accessorExpressionText, expressionText, StringComparison.Ordinal)) {
                _accessor = Property?.Compile();
                _accessorExpressionText = expressionText;
            }

            _accessorMember = member;
        }

        base.OnParametersSet();
    }

    internal override object? GetSortValue(TItem item) => _accessor is null ? null : _accessor(item);

    internal override IOrderedQueryable<TItem>? ApplyQueryableSort(IQueryable<TItem> source, SortDirection direction, bool thenBy) {
        var descending = direction == SortDirection.Descending;
        return Property is null ? null
            : thenBy && source is IOrderedQueryable<TItem> ordered ? descending ? ordered.ThenByDescending(Property) : ordered.ThenBy(Property)
            : descending ? source.OrderByDescending(Property) : source.OrderBy(Property);
    }

    internal override IOrderedEnumerable<TItem>? ApplyEnumerableSort(IEnumerable<TItem> source, SortDirection direction, bool thenBy) {
        var descending = direction == SortDirection.Descending;
        return _accessor is null ? null
            : thenBy && source is IOrderedEnumerable<TItem> ordered ? descending ? ordered.ThenByDescending(_accessor) : ordered.ThenBy(_accessor)
            : descending ? source.OrderByDescending(_accessor) : source.OrderBy(_accessor);
    }

    private protected override object? AdditionalState => (_accessorExpressionText, Format);

    private protected override object? AdditionalSortState => _accessorExpressionText;

    internal override void RenderCell(RenderTreeBuilder builder, TItem item) {
        if (CellTemplate is not null) {
            builder.AddContent(0, CellTemplate, item);
        }
        else if (_accessor is not null) {
            builder.AddContent(0, FormatValue(_accessor(item)));
        }
    }

    private string? FormatValue(TValue value) => !string.IsNullOrWhiteSpace(Format) && value is IFormattable formattable
        ? formattable.ToString(Format, CultureInfo.CurrentCulture)
        : value?.ToString();
}
