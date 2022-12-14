using System.Linq.Expressions;

namespace Utilities.Generic;

internal enum FilterToken {
	Or = 1,
	And,
	_ = 3, // ignore Not operator

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

internal readonly record struct FilterEntry(
	FilterToken Token,
	FilterEntry.PropertyRelative? Property
) {
	internal readonly record struct PropertyRelative(
		string PropertyName,
		object? FilterValue = null,
		object? SubValue = null,
		bool Not = false
	);

	private static readonly MethodInfo EnumerableContainsMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
	   .First(
			m => m is {
					Name: nameof(Enumerable.Contains)
				}
			 && m.GetParameters().Length is 2
		);

	private static readonly MethodInfo StringEqualsMethod = typeof(string).GetMethod(
		nameof(string.Equals),
		BindingFlags.Public,
		new[] { typeof(string), typeof(StringComparison) }
	)!;

	private static readonly MethodInfo StringContainsMethod = typeof(string).GetMethod(
		nameof(string.Contains),
		BindingFlags.Public,
		new[] { typeof(string), typeof(StringComparison) }
	)!;

	private static readonly MethodInfo StringStartsWithMethod = typeof(string).GetMethod(
		nameof(string.StartsWith),
		BindingFlags.Public,
		new[] { typeof(string), typeof(StringComparison) }
	)!;

	private static readonly MethodInfo StringEndsWithMethod = typeof(string).GetMethod(
		nameof(string.EndsWith),
		BindingFlags.Public,
		new[] { typeof(string), typeof(StringComparison) }
	)!;

	public Expression Convert(MemberExpression propertyAccessor) {
		var property = Property!.Value;
		var valueExpression = Expression.Constant(property.FilterValue);
		Expression e;
		switch (Token) {
			case FilterToken.Equals:
				e = property.Not
					? Expression.NotEqual(propertyAccessor, valueExpression)
					: Expression.Equal(propertyAccessor, valueExpression);
				break;
			case FilterToken.In:
				e = Expression.Call(null, EnumerableContainsMethod.MakeGenericMethod(propertyAccessor.Type), propertyAccessor, valueExpression);
				if (property.Not) {
					e = Expression.Not(e);
				}

				break;
			case FilterToken.NullOrEmpty:
				var pt = propertyAccessor.Type;
				if (pt.IsValueType && pt.Name is not "Nullable`1") {
					e = Expression.Equal(propertyAccessor, Expression.Default(pt));
				} else {
					e = Expression.Equal(propertyAccessor, Expression.Constant(null));
				}

				break;
			case FilterToken.StringEquals:
				e = Expression.Call(propertyAccessor, StringEqualsMethod, valueExpression, Expression.Constant(property.SubValue));
				break;
			case FilterToken.StringIn: goto default;
			case FilterToken.StringContains:
				e = Expression.Call(propertyAccessor, StringContainsMethod, valueExpression, Expression.Constant(property.SubValue));
				break;
			case FilterToken.StringContainsAny: goto default;
			case FilterToken.StringStartsWith:
				e = Expression.Call(propertyAccessor, StringStartsWithMethod, valueExpression, Expression.Constant(property.SubValue));
				break;
			case FilterToken.StringStartsWithAny: goto default;
			case FilterToken.StringEndsWith:
				e = Expression.Call(propertyAccessor, StringEndsWithMethod, valueExpression, Expression.Constant(property.SubValue));
				break;
			case FilterToken.StringEndsWithAny: goto default;
			default:                            throw new();
		}

		return e;
	}

	public Expression ApplyOperator(Expression left, Expression right) {
		Expression e;
		switch (Token) {
			case FilterToken.Or:
				e = Expression.OrElse(left, right);
				return e;
			case FilterToken.And:
				e = Expression.AndAlso(left, right);
				return e;
		}

		throw new();
	}
}

public sealed class BasicFilter<T> where T : notnull {
	internal static readonly Dictionary<string, PropertyInfo> AccessibleProperties = typeof(T).GetProperties().ToDictionary(x => x.Name);
	internal static readonly string TargetTypeFullName = typeof(T).FullName!;
	internal readonly FilterEntry[] Entries;

	public static readonly BasicFilter<T> Empty = new(Array.Empty<FilterEntry>());

	internal BasicFilter(FilterEntry[] entries) => Entries = entries;

	private Expression<Func<T, bool>>? lambda;
	private static readonly Expression<Func<T, bool>> Default = t => false;

	public Expression<Func<T, bool>> GetLambda() {
		if (lambda is not null) {
			return lambda;
		}

		if (Entries.Length is 0) {
			return lambda = Default;
		}

		var p = Expression.Parameter(typeof(T), "model");

		var i = 0;
		var firstFilter = Entries[i++];
		var resultExpression = Convert(firstFilter, p);

		while (i < Entries.Length) {
			var op = Entries[i++];
			var second = Entries[i++];
			var secondExpr = Convert(second, p);
			resultExpression = op.ApplyOperator(secondExpr, resultExpression);
		}

		lambda = Expression.Lambda<Func<T, bool>>(resultExpression, p);

		return lambda;
	}

	public BasicFilter<TOther> Cast<TOther>() where TOther : notnull => new(Entries);

	private static Expression Convert(FilterEntry entry, Expression modelAccessor) {
		var propertyAccessor = Expression.MakeMemberAccess(modelAccessor, AccessibleProperties[entry.Property!.Value.PropertyName]);
		return entry.Convert(propertyAccessor);
	}
}
