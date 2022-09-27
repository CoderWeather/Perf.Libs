using System.Linq.Expressions;

namespace Utilities.Generic;

partial class BasicFilterBuilder<T> {
	public readonly struct ForPropertyWrapper<TProperty> {
		public ForPropertyWrapper(BasicFilterBuilder<T> builder, Expression<Func<T, TProperty>> accessor) {
			Builder = builder;
			Accessor = accessor;
		}

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

		private BasicFilterBuilder<T> Builder { get; init; }
		private Expression<Func<T, TProperty>> Accessor { get; init; }

		public IIntermediate Equals(TProperty value) => Builder.Push(Accessor, FilterBuilderToken.Equals, value);
		public IIntermediate In(TProperty[] values) => Builder.Push(Accessor, FilterBuilderToken.In, values);
		public IIntermediate NullOrEmpty() => Builder.Push(Accessor, FilterBuilderToken.NullOrEmpty);
	}

	public readonly struct ForStringPropertyWrapper {
		public ForStringPropertyWrapper(BasicFilterBuilder<T> builder, Expression<Func<T, string>> accessor) {
			Builder = builder;
			Accessor = accessor;
		}

		private BasicFilterBuilder<T> Builder { get; init; }
		private Expression<Func<T, string>> Accessor { get; init; }

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

		public IIntermediate Equals(string value, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringEquals, value, comparison);
		}

		public IIntermediate In(string[] values, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringIn, values, comparison);
		}

		public IIntermediate NullOrEmpty() => Builder.Push(Accessor, FilterBuilderToken.NullOrEmpty);

		public IIntermediate StartsWith(string value, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringStartsWith, value, comparison);
		}

		public IIntermediate StartsWithAny(string[] values, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringStartsWithAny, values, comparison);
		}

		public IIntermediate EndsWith(string value, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringEndsWith, value, comparison);
		}

		public IIntermediate EndsWithAny(string[] values, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringEndsWithAny, values, comparison);
		}

		public IIntermediate Contains(string value, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringContains, value, comparison);
		}

		public IIntermediate ContainsAny(string[] values, StringComparison comparison = StringComparison.Ordinal) {
			return Builder.Push(Accessor, FilterBuilderToken.StringContainsAny, values, comparison);
		}
	}
}
