using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace Utilities.Generic;

public sealed class UpdateModelException : Exception {
	public UpdateModelException(string? message = null) : base(message) { }
}

public sealed class Update<T> where T : notnull, new() {
	internal static readonly Dictionary<string, PropertyInfo> PropNameToProperties = Typeof<T>().GetProperties().ToDictionary(x => x.Name);

	private readonly Dictionary<PropertyInfo, (Type Type, object? Value, bool InnerUpdate)> values;
	internal readonly Dictionary<string, (string TypeFullName, object? Value, bool InnerUpdate)> SerializableValues;

	public static readonly Update<T> Empty = new();

	private Update() {
		values = new(0);
		SerializableValues = new(0);
	}

	internal Update(Dictionary<PropertyInfo, (Type Type, object? Value, bool InnerUpdate)> values) {
		this.values = values;
		SerializableValues = new(this.values.Count);
		foreach (var (k, v) in this.values) {
			SerializableValues[k.Name] = (v.Type.FullName!, v.Value, v.InnerUpdate);
		}
	}

	internal Update(Dictionary<string, (string TypeFullName, object? Value, bool InnerUpdate)> serializableValues) {
		SerializableValues = serializableValues;
		values = new(SerializableValues.Count);
		foreach (var (k, v) in SerializableValues) {
			if (PropNameToProperties.TryGetValue(k, out var propInfo) is false) {
				throw new UpdateModelException(); // own exception with description
			}

			if (Type.GetType(v.TypeFullName) is not { } t) {
				throw new UpdateModelException(); // own exception with description
			}

			values.Add(propInfo, (t, v.Value, v.InnerUpdate));
		}
	}

	public bool Contains<TMember>(Expression<Func<T, TMember>> accessor) =>
		accessor is {
			NodeType: ExpressionType.Lambda,
			Body: MemberExpression {
				NodeType: ExpressionType.MemberAccess,
				Member: PropertyInfo property,
				Type: { }
			}
		}
	 && values.ContainsKey(property);

	public bool TryGetValue<TMember>(Expression<Func<T, TMember>> accessor, out object? value) {
		if (accessor is {
			    NodeType: ExpressionType.Lambda,
			    Body: MemberExpression {
				    NodeType: ExpressionType.MemberAccess,
				    Member: PropertyInfo property,
				    Type: { }
			    }
		    }
		 && values.TryGetValue(property, out var tuple)) {
			value = tuple.Value;
			return true;
		}

		value = null;
		return false;
	}

	public Update<T2> Cast<T2>() where T2 : notnull, new() {
		var thisProps = PropNameToProperties;
		var otherProps = Update<T2>.PropNameToProperties;

		var newValues = new Dictionary<PropertyInfo, (Type Type, object? Value, bool InnerUpdate)>(values.Count);

		using var thisPropsIter = thisProps.GetEnumerator();
		while (thisPropsIter.MoveNext()) {
			var (k, v) = thisPropsIter.Current;
			if (otherProps.TryGetValue(k, out var v2) is false) {
				continue;
			}

			var vv = values[v];

			if (v.PropertyType != v2.PropertyType) {
				if (vv.Value is null || vv.InnerUpdate is false) {
					continue;
				}

				var cast = Update.GetUpdateCastFunc(v.PropertyType, v2.PropertyType);
				var newObjectValue = cast.Invoke(vv.Value);
				vv = (vv.Type, newObjectValue, true);
			}

			newValues[v2] = vv;
		}

		return new(newValues);
	}

	private MemberInitExpression? updateLambdaBody;
	private Expression<Func<T, T>>? updateLambda;

	private MemberInitExpression GetUpdateLambdaBody(Expression modelAccessor) {
		if (updateLambdaBody is null) {
			_ = modelAccessor;
			updateLambdaBody = Expression.MemberInit(
				Expression.New(Typeof<T>()),
				values.Select(
					kv => {
						var v = kv.Value;
						Expression bindValue;
						if (kv.Value.InnerUpdate && v.Value is not null) {
							var innerLambdaBody = Update.GetUpdateLambdaBodyFunc(v.Type);
							var body = innerLambdaBody.Invoke(v.Value, Expression.MakeMemberAccess(modelAccessor, kv.Key));
							bindValue = body;
						} else {
							bindValue = Expression.Constant(v.Value);
						}

						return Expression.Bind(kv.Key, bindValue);
					}
				)
			);
		}

		return updateLambdaBody;
	}

	public Expression<Func<T, T>> GetUpdateLambda() {
		if (updateLambda is null) {
			var p = Expression.Parameter(Typeof<T>());
			updateLambda = Expression.Lambda<Func<T, T>>(GetUpdateLambdaBody(p), p);
		}

		return updateLambda;
	}
}

public static class Update {
	public static Update<T> Empty<T>() where T : notnull, new() => Update<T>.Empty;
	public static UpdateBuilder<T> Create<T>() where T : notnull, new() => new();
	public static (string Name, string Type)[] AccessibleProperties<T>() where T : notnull, new() => Cache<T>.AccessibleProperties;

	[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
	private static class Cache<T> where T : notnull, new() {
		public static readonly (string Name, string Type)[] AccessibleProperties;

		static Cache() {
			AccessibleProperties = Update<T>.PropNameToProperties.Select(x => (x.Value.Name, x.Value.PropertyType.Name)).ToArray();
		}
	}

	internal static bool IsUpdateModel(this Type type) => type.Name is "Update`1";

	private static readonly Dictionary<(Type From, Type To), Func<object, object>> UpdateCastFuncCache = new();

	internal static Func<object, object> GetUpdateCastFunc(Type from, Type to) {
		if (UpdateCastFuncCache.TryGetValue((from, to), out var func)) {
			return func;
		}

		var parameter = Expression.Parameter(Typeof<object>(), "from");
		var fromUpdateType = typeof(Update<>).MakeGenericType(from);
		var lambda = Expression.Lambda<Func<object, object>>(
			Expression.Convert(
				Expression.Call(
					Expression.Convert(parameter, fromUpdateType),
					fromUpdateType.GetMethod("Cast")!.MakeGenericMethod(to)
				),
				Typeof<object>()
			),
			parameter
		);

		UpdateCastFuncCache[(from, to)] = func = lambda.CompileFast();
		return func;
	}

	private static readonly Dictionary<Type, Func<object, Expression, Expression>> UpdateGetUpdateLambdaBodyCache = new();

	internal static Func<object, Expression, Expression> GetUpdateLambdaBodyFunc(Type type) {
		if (UpdateGetUpdateLambdaBodyCache.TryGetValue(type, out var func)) {
			return func;
		}

		var update = Expression.Parameter(Typeof<object>(), "update");
		var updateType = typeof(Update<>).MakeGenericType(type);
		var modelAccessor = Expression.Parameter(Typeof<Expression>(), "modelAccessor");
		var method = updateType.GetMethod("GetUpdateLambdaBody", BindingFlags.NonPublic)!;

		var lambda = Expression.Lambda<Func<object, Expression, Expression>>(
			Expression.Convert(
				Expression.Call(
					Expression.Convert(update, updateType),
					method,
					modelAccessor
				),
				Typeof<Expression>()
			),
			update,
			modelAccessor
		);

		UpdateGetUpdateLambdaBodyCache[type] = func = lambda.CompileFast();
		return func;
	}
}
