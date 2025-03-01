using System.Linq.Expressions;

namespace Utilities.Generic;

using JetBrains.Annotations;

partial class BasicFilterBuilder<T> {
    public readonly struct ForPropertyWrapper<TProperty>(
        BasicFilterBuilder<T> builder,
        Expression<Func<T, TProperty>> accessor
    ) {
        [UsedImplicitly]
        public ForPropertyWrapper<TProperty> Not {
            get {
                // remove 'Not' token from stack if exists
                // 'not not' duplicate remove
                if (Builder.operations.TryPop(FilterBuilderOperationToken.Not) is false) {
                    Builder.operations.Push(FilterBuilderOperationToken.Not);
                }

                return this;
            }
        }

        private BasicFilterBuilder<T> Builder { get; init; } = builder;
        private Expression<Func<T, TProperty>> Accessor { get; init; } = accessor;

        [UsedImplicitly]
        public IIntermediate Equals(TProperty value) => Builder.Push(Accessor, FilterBuilderToken.Equals, value);

        [UsedImplicitly]
        public IIntermediate In(TProperty[] values) => Builder.Push(Accessor, FilterBuilderToken.In, values);

        [UsedImplicitly]
        public IIntermediate NullOrEmpty() => Builder.Push(Accessor, FilterBuilderToken.NullOrEmpty);
    }

    public readonly struct ForStringPropertyWrapper(
        BasicFilterBuilder<T> builder,
        Expression<Func<T, string>> accessor
    ) {
        [UsedImplicitly]
        private BasicFilterBuilder<T> Builder { get; init; } = builder;
        [UsedImplicitly]
        private Expression<Func<T, string>> Accessor { get; init; } = accessor;

        [UsedImplicitly]
        public ForStringPropertyWrapper Not {
            get {
                // remove 'Not' token from stack if exists
                // 'not not' duplicate remove
                if (Builder.operations.TryPop(FilterBuilderOperationToken.Not) is false) {
                    Builder.operations.Push(FilterBuilderOperationToken.Not);
                }

                return this;
            }
        }

        [UsedImplicitly]
        public IIntermediate Equals(string value, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringEquals, value, comparison);
        }

        [UsedImplicitly]
        public IIntermediate In(string[] values, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringIn, values, comparison);
        }

        [UsedImplicitly]
        public IIntermediate NullOrEmpty() => Builder.Push(Accessor, FilterBuilderToken.NullOrEmpty);

        [UsedImplicitly]
        public IIntermediate StartsWith(string value, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringStartsWith, value, comparison);
        }

        [UsedImplicitly]
        public IIntermediate StartsWithAny(string[] values, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringStartsWithAny, values, comparison);
        }

        [UsedImplicitly]
        public IIntermediate EndsWith(string value, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringEndsWith, value, comparison);
        }

        [UsedImplicitly]
        public IIntermediate EndsWithAny(string[] values, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringEndsWithAny, values, comparison);
        }

        [UsedImplicitly]
        public IIntermediate Contains(string value, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringContains, value, comparison);
        }

        [UsedImplicitly]
        public IIntermediate ContainsAny(string[] values, StringComparison comparison = StringComparison.Ordinal) {
            return Builder.Push(Accessor, FilterBuilderToken.StringContainsAny, values, comparison);
        }
    }
}
