using System.Linq.Expressions;

namespace NTComponents;

/// <summary>    Represents sorting rules for an <see cref="NTDataGrid{TItem}" /> column.</summary>
/// <typeparam name="TItem">The type of item displayed by the grid.</typeparam>
public sealed class NTGridSort<TItem> where TItem : class {
    private const string _expressionNotRepresentableMessage = "The supplied expression cannot be represented as a property name for sorting. Only member expressions, such as @(item => item.SomeProperty), are supported.";

    private readonly ISortClause _first;
    private readonly List<ISortClause> _then = [];

    private NTGridSort(ISortClause first) {
        _first = first;
        StateSignature = CreateStateSignature(first);
    }

    /// <summary>    Creates sorting rules that sort by the supplied expression in ascending order.</summary>
    /// <typeparam name="TValue">The type returned by the expression.</typeparam>
    /// <param name="expression">The expression used to select the sort value.</param>
    /// <returns>The configured sorting rules.</returns>
    public static NTGridSort<TItem> ByAscending<TValue>(Expression<Func<TItem, TValue>> expression) => Create(expression, true);

    /// <summary>    Creates sorting rules that sort by the supplied expression in descending order.</summary>
    /// <typeparam name="TValue">The type returned by the expression.</typeparam>
    /// <param name="expression">The expression used to select the sort value.</param>
    /// <returns>The configured sorting rules.</returns>
    public static NTGridSort<TItem> ByDescending<TValue>(Expression<Func<TItem, TValue>> expression) => Create(expression, false);

    /// <summary>    Appends an ascending sorting rule.</summary>
    /// <typeparam name="TValue">The type returned by the expression.</typeparam>
    /// <param name="expression">The expression used to select the sort value.</param>
    /// <returns>The updated sorting rules.</returns>
    public NTGridSort<TItem> ThenAscending<TValue>(Expression<Func<TItem, TValue>> expression) => Add(expression, true);

    /// <summary>    Appends a descending sorting rule.</summary>
    /// <typeparam name="TValue">The type returned by the expression.</typeparam>
    /// <param name="expression">The expression used to select the sort value.</param>
    /// <returns>The updated sorting rules.</returns>
    public NTGridSort<TItem> ThenDescending<TValue>(Expression<Func<TItem, TValue>> expression) => Add(expression, false);

    internal string PropertyName => _first.PropertyName;

    internal SortDirection DefaultDirection => _first.Ascending ? SortDirection.Ascending : SortDirection.Descending;

    internal string StateSignature { get; private set; }

    internal IOrderedEnumerable<TItem> Apply(IEnumerable<TItem> source, SortDirection direction, bool thenBy) {
        var useConfiguredDirections = direction == DefaultDirection;
        return _then.Aggregate(_first.Apply(source, useConfiguredDirections, thenBy), (ordered, clause) => clause.Apply(ordered, useConfiguredDirections, true));
    }

    internal IOrderedQueryable<TItem> Apply(IQueryable<TItem> source, SortDirection direction, bool thenBy) {
        var useConfiguredDirections = direction == DefaultDirection;
        return _then.Aggregate(_first.Apply(source, useConfiguredDirections, thenBy), (ordered, clause) => clause.Apply(ordered, useConfiguredDirections, true));
    }

    internal IReadOnlyList<NTSortDescriptor> GetSortDescriptors(SortDirection direction) {
        var useConfiguredDirections = direction == DefaultDirection;
        return [CreateSortDescriptor(_first, useConfiguredDirections), .. _then.Select(clause => CreateSortDescriptor(clause, useConfiguredDirections))];
    }

    private static NTGridSort<TItem> Create<TValue>(Expression<Func<TItem, TValue>> expression, bool ascending) {
        ArgumentNullException.ThrowIfNull(expression);
        return new NTGridSort<TItem>(new SortClause<TValue>(expression, ascending));
    }

    private NTGridSort<TItem> Add<TValue>(Expression<Func<TItem, TValue>> expression, bool ascending) {
        ArgumentNullException.ThrowIfNull(expression);
        var clause = new SortClause<TValue>(expression, ascending);
        _then.Add(clause);
        StateSignature = string.Concat(StateSignature, "|", CreateStateSignature(clause));
        return this;
    }

    private static NTSortDescriptor CreateSortDescriptor(ISortClause clause, bool useConfiguredDirections) =>
        new(clause.PropertyName, clause.Ascending == useConfiguredDirections ? SortDirection.Ascending : SortDirection.Descending);

    private static string CreateStateSignature(ISortClause clause) => string.Concat(clause.PropertyName, ":", clause.Ascending ? "asc" : "desc");

    private interface ISortClause {
        string PropertyName { get; }

        bool Ascending { get; }

        IOrderedEnumerable<TItem> Apply(IEnumerable<TItem> source, bool useConfiguredDirections, bool thenBy);

        IOrderedQueryable<TItem> Apply(IQueryable<TItem> source, bool useConfiguredDirections, bool thenBy);
    }

    private sealed class SortClause<TValue>(Expression<Func<TItem, TValue>> _expression, bool _ascending) : ISortClause {
        private Func<TItem, TValue>? _accessor;

        public string PropertyName { get; } = ToPropertyName(_expression);

        public bool Ascending => _ascending;

        public IOrderedEnumerable<TItem> Apply(IEnumerable<TItem> source, bool useConfiguredDirections, bool thenBy) {
            var accessor = _accessor ??= _expression.Compile(preferInterpretation: true);
            if (thenBy && source is IOrderedEnumerable<TItem> orderedSource) {
                return useConfiguredDirections == _ascending ? orderedSource.ThenBy(accessor) : orderedSource.ThenByDescending(accessor);
            }

            return useConfiguredDirections == _ascending ? source.OrderBy(accessor) : source.OrderByDescending(accessor);
        }

        public IOrderedQueryable<TItem> Apply(IQueryable<TItem> source, bool useConfiguredDirections, bool thenBy) {
            if (thenBy && source is IOrderedQueryable<TItem> orderedSource) {
                return useConfiguredDirections == _ascending ? orderedSource.ThenBy(_expression) : orderedSource.ThenByDescending(_expression);
            }

            return useConfiguredDirections == _ascending ? source.OrderBy(_expression) : source.OrderByDescending(_expression);
        }
    }

    private static string ToPropertyName(LambdaExpression expression) {
        var expressionBody = expression.Body;
        if (expressionBody.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked) {
            expressionBody = ((UnaryExpression)expressionBody).Operand;
        }

        if (expressionBody is not MemberExpression body) {
            throw new ArgumentException(_expressionNotRepresentableMessage, nameof(expression));
        }

        var members = new Stack<string>();
        Expression? current = body;
        while (current is MemberExpression member) {
            members.Push(member.Member.Name);
            current = member.Expression;
        }

        if (current is not ParameterExpression) {
            throw new ArgumentException(_expressionNotRepresentableMessage, nameof(expression));
        }

        return string.Join('.', members);
    }
}
