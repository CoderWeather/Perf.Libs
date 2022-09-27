using System.Linq.Expressions;
using System.Reflection;

namespace Utilities.Generic;

public sealed class UpdateBuilder<T> where T : notnull, new() {
	private readonly Dictionary<PropertyInfo, (Type Type, object? Value, bool InnerUpdate)> values = new(8);

	public UpdateBuilder<T> Set<TMember>(Expression<Func<T, TMember>> accessor, TMember value) {
		if (accessor is {
				NodeType: ExpressionType.Lambda,
				Body: MemberExpression {
					NodeType: ExpressionType.MemberAccess,
					Member: PropertyInfo property,
					Type: { } propertyType
				}
			}) {
			values[property] = (propertyType, value, false);
			return this;
		}

		throw new UpdateModelException(); // own exception with description
	}

	public UpdateBuilder<T> SetWithUpdate<TMember>(Expression<Func<T, TMember>> accessor, Action<UpdateBuilder<TMember>> memberUpdateBuilder)
		where TMember : notnull, new() {
		if (accessor is {
				NodeType: ExpressionType.Lambda,
				Body: MemberExpression {
					NodeType: ExpressionType.MemberAccess,
					Member: PropertyInfo property,
					Type: { } propertyType
				}
			}) {
			var memberBuilder = UpdateBuilder.Create<TMember>();
			memberUpdateBuilder.Invoke(memberBuilder);
			var memberUpdate = memberBuilder.Build();
			values[property] = (propertyType, memberUpdate, true);
			return this;
		}

		throw new UpdateModelException();
	}

	internal UpdateBuilder<T> FullUnsafe(Expression<Func<T, T>> lambda) {
		// var s = "some string";
		// .Full(x => new() { x.Name = s })
		if (lambda is not {
				NodeType: ExpressionType.Lambda,
				Body: MemberInitExpression {
					NodeType: ExpressionType.MemberInit,
					Bindings: { Count: > 0 } bindings
				}
			}) {
			return this;
		}

		var parameter = lambda.Parameters[0];

		foreach (var b in bindings) {
			if (b is MemberAssignment {
					BindingType: MemberBindingType.Assignment,
					Member: PropertyInfo property,
					Expression: { } valueExpression
				}) {
				switch (valueExpression) {
					case ConstantExpression ce:
						// Any already calculated value can be used
						values[property] = (ce.Type, ce.Value, false);
						break;
					// // Method call or chain call, only when root value provided by model property
					// // When method is static and method object is real constant value
					// case MethodCallExpression mce:
					// 	var method = mce.Method;
					// 	if (mce.Object is Expression o) { }
					//
					// 	break;
					case MemberExpression {
						// Own property simple access {Value = x.Value}
						NodeType: ExpressionType.MemberAccess,
						Expression: ParameterExpression pe,
						Type: { },
						Member: PropertyInfo valueProperty
					} when pe == parameter && valueProperty == property:
						break;

					// var x = 10;
					// .Full(x => new() { x.Name = x });
					case MemberExpression me:
						var stack = new Stack<MemberInfo>();
						Expression e = me;
						while (e is MemberExpression {
								   Member: FieldInfo or PropertyInfo,
								   Expression: { } and not ConstantExpression
							   } current
							  ) {
							stack.Push(current.Member);
							e = current.Expression;
						}

						if (stack.Count is 0 || e is not MemberExpression meLast) {
							break;
						}

						stack.Push(meLast.Member);
						if (e is MemberExpression { Expression: ConstantExpression constValue }) {
							try {
								var val = constValue.Value;
								Type lastReturnType = null!;
								while (stack.TryPop(out var member))
									switch (member) {
										case PropertyInfo pi:
											val = pi.GetValue(val);
											lastReturnType = pi.PropertyType;
											break;
										case FieldInfo fi:
											val = fi.GetValue(val);
											lastReturnType = fi.FieldType;
											break;
										default: throw new ArgumentOutOfRangeException();
									}

								values[property] = (lastReturnType, val, false);
							} catch (Exception) {
								throw new UpdateModelException(); // own exception with description
							}
						}

						break;
					default: throw new UpdateModelException(); // own exception with description
				}
			}

			return this;
		}

		throw new UpdateModelException(); // own exception with description
	}

	public Update<T> Build() => new(values);
}

public static class UpdateBuilder {
	public static UpdateBuilder<T> Create<T>() where T : notnull, new() => new();
}
