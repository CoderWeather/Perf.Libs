using System.Diagnostics;
using System.Linq.Expressions;

namespace Utilities.Generic;

using JetBrains.Annotations;

partial class BasicFilterBuilder<T> {
    public interface IStart {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [UsedImplicitly]
        IStart Not { get; }

        [UsedImplicitly]
        ForPropertyWrapper<TProperty> For<TProperty>(Expression<Func<T, TProperty>> accessor);
        [UsedImplicitly]
        ForStringPropertyWrapper For(Expression<Func<T, string>> accessor);

        [UsedImplicitly]
        IIntermediate Equals<TProperty>(Expression<Func<T, TProperty>> accessor, TProperty value);
        [UsedImplicitly]
        IIntermediate In<TProperty>(Expression<Func<T, TProperty>> accessor, params TProperty[] values);
        [UsedImplicitly]
        IIntermediate NullOrEmpty<TProperty>(Expression<Func<T, TProperty>> accessor);
        [UsedImplicitly]
        IIntermediate Equals(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

        [UsedImplicitly]
        IIntermediate In(Expression<Func<T, string>> accessor, StringComparison comparison = StringComparison.Ordinal, params string[] values);

        [UsedImplicitly]
        IIntermediate StartsWith(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

        [UsedImplicitly]
        IIntermediate StartsWithAny(
            Expression<Func<T, string>> accessor,
            StringComparison comparison = StringComparison.Ordinal,
            params string[] values
        );

        [UsedImplicitly]
        IIntermediate EndsWith(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

        [UsedImplicitly]
        IIntermediate EndsWithAny(
            Expression<Func<T, string>> accessor,
            StringComparison comparison = StringComparison.Ordinal,
            params string[] values
        );

        [UsedImplicitly]
        IIntermediate Contains(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

        [UsedImplicitly]
        IIntermediate ContainsAny(
            Expression<Func<T, string>> accessor,
            StringComparison comparison = StringComparison.Ordinal,
            params string[] values
        );
    }

    public interface IIntermediate {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [UsedImplicitly]
        IStart And { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [UsedImplicitly]
        IStart Or { get; }

        [UsedImplicitly]
        BasicFilter<T> Build();
    }
}

partial class BasicFilterBuilder<T>
    : BasicFilterBuilder<T>.IStart, BasicFilterBuilder<T>.IIntermediate {
    IStart IStart.Not {
        get {
            // remove 'Not' token from stack if exists
            // 'not not' duplicate remove
            if (operations.TryPop(FilterBuilderOperationToken.Not) is false) {
                operations.Push(FilterBuilderOperationToken.Not);
            }

            return this;
        }
    }

    ForPropertyWrapper<TProperty> IStart.For<TProperty>(Expression<Func<T, TProperty>> accessor) => new(this, accessor);

    ForStringPropertyWrapper IStart.For(Expression<Func<T, string>> accessor) => new(this, accessor);

    IIntermediate IStart.Equals<TProperty>(Expression<Func<T, TProperty>> accessor, TProperty value) {
        return Push(accessor, FilterBuilderToken.Equals, value);
    }

    IIntermediate IStart.In<TProperty>(Expression<Func<T, TProperty>> accessor, params TProperty[] values) {
        return Push(accessor, FilterBuilderToken.In, values);
    }

    IIntermediate IStart.NullOrEmpty<TProperty>(Expression<Func<T, TProperty>> accessor) {
        return Push(accessor, FilterBuilderToken.NullOrEmpty);
    }

    IIntermediate IStart.Equals(Expression<Func<T, string>> accessor, string value, StringComparison comparison) {
        return Push(accessor, FilterBuilderToken.StringEquals, value, comparison);
    }

    IIntermediate IStart.In(Expression<Func<T, string>> accessor, StringComparison comparison, params string[] values) {
        return Push(accessor, FilterBuilderToken.StringIn, values, comparison);
    }

    IIntermediate IStart.StartsWith(Expression<Func<T, string>> accessor, string value, StringComparison comparison) {
        return Push(accessor, FilterBuilderToken.StringStartsWith, value, comparison);
    }

    IIntermediate IStart.StartsWithAny(Expression<Func<T, string>> accessor, StringComparison comparison, params string[] values) {
        return Push(accessor, FilterBuilderToken.StringStartsWithAny, values, comparison);
    }

    IIntermediate IStart.EndsWith(Expression<Func<T, string>> accessor, string value, StringComparison comparison) {
        return Push(accessor, FilterBuilderToken.StringEndsWith, value, comparison);
    }

    IIntermediate IStart.EndsWithAny(Expression<Func<T, string>> accessor, StringComparison comparison, params string[] values) {
        return Push(accessor, FilterBuilderToken.StringEndsWithAny, values, comparison);
    }

    IIntermediate IStart.Contains(Expression<Func<T, string>> accessor, string value, StringComparison comparison) {
        return Push(accessor, FilterBuilderToken.StringContains, value, comparison);
    }

    IIntermediate IStart.ContainsAny(Expression<Func<T, string>> accessor, StringComparison comparison, params string[] values) {
        return Push(accessor, FilterBuilderToken.StringContainsAny, values, comparison);
    }

    IStart IIntermediate.And {
        get {
            operations.Push(FilterBuilderOperationToken.And);
            return this;
        }
    }

    IStart IIntermediate.Or {
        get {
            operations.Push(FilterBuilderOperationToken.Or);
            return this;
        }
    }
}
