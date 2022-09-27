using System.Diagnostics;
using System.Linq.Expressions;

namespace Utilities.Generic;

partial class BasicFilterBuilder<T> {
	public interface IStart {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IStart Not { get; }

		ForPropertyWrapper<TProperty> For<TProperty>(Expression<Func<T, TProperty>> accessor);
		ForStringPropertyWrapper For(Expression<Func<T, string>> accessor);

		IIntermediate Equals<TProperty>(Expression<Func<T, TProperty>> accessor, TProperty value);
		IIntermediate In<TProperty>(Expression<Func<T, TProperty>> accessor, params TProperty[] values);
		IIntermediate NullOrEmpty<TProperty>(Expression<Func<T, TProperty>> accessor);
		IIntermediate Equals(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

		IIntermediate In(Expression<Func<T, string>> accessor, StringComparison comparison = StringComparison.Ordinal, params string[] values);

		IIntermediate StartsWith(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

		IIntermediate StartsWithAny(Expression<Func<T, string>> accessor,
			StringComparison comparison = StringComparison.Ordinal,
			params string[] values);

		IIntermediate EndsWith(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

		IIntermediate EndsWithAny(Expression<Func<T, string>> accessor,
			StringComparison comparison = StringComparison.Ordinal,
			params string[] values);

		IIntermediate Contains(Expression<Func<T, string>> accessor, string value, StringComparison comparison = StringComparison.Ordinal);

		IIntermediate ContainsAny(Expression<Func<T, string>> accessor,
			StringComparison comparison = StringComparison.Ordinal,
			params string[] values);
	}

	public interface IIntermediate {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IStart And { get; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IStart Or { get; }

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
