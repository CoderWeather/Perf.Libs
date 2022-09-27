using System.Linq.Expressions;
using System.Reflection;

namespace Utilities.Generic;

// Builder – combination of just serializable values
// Builder->Filter --> serializable values only
// Filter --> caching result Expression when attempt to build

internal enum FilterBuilderOperationToken {
	Or = 1,
	And,
	Not
}

internal enum FilterBuilderToken {
	Equals = 10,
	In,
	NullOrEmpty,

	// string
	StringEquals,
	StringIn,
	StringContains,
	StringContainsAny,
	StringStartsWith,
	StringStartsWithAny,
	StringEndsWith,
	StringEndsWithAny
}

internal readonly record struct FilterBuilderEntry(
	FilterBuilderToken Token,
	string PropertyName,
	object? FilterValue = null,
	object? SubValue = null,
	bool Not = false
);

public sealed partial class BasicFilterBuilder<T> where T : notnull {
	private readonly Stack<FilterBuilderEntry> entries = new();
	private readonly Stack<FilterBuilderOperationToken> operations = new();
	private BasicFilterBuilder() { }

	internal static BasicFilterBuilder<T> Create() => new();

	private BasicFilterBuilder<T> Push<TProperty>(Expression<Func<T, TProperty>> accessor,
		FilterBuilderToken token,
		object? value = null,
		object? subValue = null) {
		var property = accessor.TakeProperty();
		var propertyName = property.Name;
		entries.Push(
			new(token, propertyName, value, subValue) {
				Not = operations.TryPop(FilterBuilderOperationToken.Not)
			}
		);

		return this;
	}

	BasicFilter<T> IIntermediate.Build() {
		if (entries.Count is 0) {
			return BasicFilter<T>.Empty;
		}

		var resultEntries = new FilterEntry[entries.Count + operations.Count];
		var i = 0;

		var first = entries.Pop();
		resultEntries[i++] = new((FilterToken)first.Token, new(first.PropertyName, first.FilterValue, first.SubValue, first.Not));

		while (operations.TryPop(out var op)) {
			var nextEntry = entries.Pop();

			resultEntries[i++] = MapOperation(op);
			resultEntries[i++] = Map(nextEntry);
		}

		return new(resultEntries);

		static FilterEntry Map(FilterBuilderEntry entry) {
			return new((FilterToken)entry.Token, new(entry.PropertyName, entry.FilterValue, entry.SubValue, entry.Not));
		}

		static FilterEntry MapOperation(FilterBuilderOperationToken operation) {
			return new((FilterToken)operation, default);
		}
	}
}

public static class BasicFilterBuilder {
	public static BasicFilterBuilder<T>.IStart Create<T>() where T : notnull => BasicFilterBuilder<T>.Create();

	internal static PropertyInfo TakeProperty<TModel, TProperty>(this Expression<Func<TModel, TProperty>> propertySelector) {
		if (propertySelector is {
				NodeType: ExpressionType.Lambda,
				Body: MemberExpression {
					NodeType: ExpressionType.MemberAccess,
					Member: PropertyInfo property,
					Type: { }
				}
			}) {
			return property;
		}

		throw new();
	}

	internal static bool TryPop(this Stack<FilterBuilderOperationToken> stack, FilterBuilderOperationToken token) {
		return stack.TryPopWhen(x => x == token, out _);
	}
}
