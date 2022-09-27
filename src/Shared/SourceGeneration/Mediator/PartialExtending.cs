namespace Perf.SourceGeneration.Mediator;

[Generator]
public sealed class MediatorPartialExtending : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		#region Mirror Messages

		var mirrorMessagesToPartialExtend = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, ct) => {
				if (node is TypeDeclarationSyntax {
						BaseList.Types.Count: > 0,
						TypeParameterList: null,
						Keyword.Text: "class" or "record" or "struct"
					} t
				 && t.Modifiers.Any(SyntaxKind.PartialKeyword)) {
					foreach (var bt in t.BaseList.Types) {
						if (bt is SimpleBaseTypeSyntax {
								Type: GenericNameSyntax {
									Identifier.Text: "IMirrorRequest" or "IMirrorCommand" or "IMirrorQuery",
									TypeArgumentList.Arguments.Count: 1 or 2
								}
							}) {
							return true;
						}
					}
				}

				return false;
			},
			static (context, ct) => {
				var syntax = (TypeDeclarationSyntax)context.Node;

				if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } type) {
					return default;
				}

				foreach (var i in type.Interfaces) {
					switch (i.FullPath()) {
						case "Utilities.Mediator.Messages.IMirrorRequest": {
							var input = type;
							var originInput = i.TypeArguments[0];
							var originOutput = originInput.Interfaces.First(x => x.Name is "IRequest" or "ICommand" or "IQuery").TypeArguments[0];
							var output = i.TypeArguments.Length is 2 ? i.TypeArguments[1] : originOutput;
							return new(MessageType.Request, originInput, input, originOutput, output);
						}
						case "Utilities.Mediator.Messages.IMirrorCommand": {
							var input = type;
							var originInput = i.TypeArguments[0];
							var originOutput = originInput.Interfaces.First(x => x.Name is "IRequest" or "ICommand" or "IQuery").TypeArguments[0];
							var output = i.TypeArguments.Length is 2 ? i.TypeArguments[1] : originOutput;
							return new(MessageType.Command, originInput, input, originOutput, output);
						}
						case "Utilities.Mediator.Messages.IMirrorQuery": {
							var input = type;
							var originInput = i.TypeArguments[0];
							var originOutput = originInput.Interfaces.First(x => x.Name is "IRequest" or "ICommand" or "IQuery").TypeArguments[0];
							var output = i.TypeArguments.Length is 2 ? i.TypeArguments[1] : originOutput;
							return new(MessageType.Query, originInput, input, originOutput, output);
						}
						default: continue;
					}
				}

				return default(WrappableMirrorMessage);
			}
		);

		context.RegisterSourceOutput(
			mirrorMessagesToPartialExtend.Collect(),
			static (context, messages) => {
				if (messages.IsDefaultOrEmpty) {
					return;
				}

				using var writer = new IndentedTextWriter(new StringWriter(), "	");

				writer.WriteLines(
					"// <auto-generated />",
					"#pragma warning disable CS8019",
					"#pragma warning disable CS0105",
					"#nullable enable",
					null,
					"using Utilities.Mediator.Messages;",
					null
				);

				foreach (var group in messages.GroupBy(x => x.Input.ContainingNamespace, SymbolEqualityComparer.Default)) {
					var ns = (INamespaceSymbol)group.Key!;

					writer.WriteLine($"namespace {ns.ToDisplayString()} {{");
					foreach (var m in group) {
						var ending = m.Input.IsRecord ? ";" : " {}";
						writer.WriteLine(
							$"partial {m.Input.DeclarationString()} {m.Input.Name} : {m.MessageInterfaceTypeText}{ending}"
						);
					}

					writer.WriteLine("}");
				}


				var sourceCode = writer.InnerWriter.ToString()!;

				context.AddSource("Mediator_Mirror_Messages.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
			}
		);

		#endregion

		#region Handlers

		var handlersToExtendWithPartials = context.SyntaxProvider.CreateSyntaxProvider(
				static (node, ct) => {
					if (node is TypeDeclarationSyntax {
							BaseList.Types.Count: > 0,
							TypeParameterList: null,
							Keyword.Text: "class" or "record"
						} t
					 && t.Modifiers.Any(SyntaxKind.PartialKeyword)) {
						foreach (var bt in t.BaseList.Types) {
							if (bt is SimpleBaseTypeSyntax {
									Type: GenericNameSyntax {
										Identifier.Text: "IMirrorRequestHandler"
										or "IRequestHandler"
										or "IScopedRequestHandler"
										or "IMirrorCommandHandler"
										or "ICommandHandler"
										or "IScopedCommandHandler"
										or "IMirrorQueryHandler"
										or "IQueryHandler"
										or "IScopedQueryHandler",
										TypeArgumentList.Arguments.Count: 1
									}
								}) {
								return true;
							}
						}
					}

					return false;
				},
				static (context, ct) => {
					var syntax = (TypeDeclarationSyntax)context.Node;

					if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } type) {
						return default;
					}

					var h = new Handler(type);

					foreach (var i in type.Interfaces) {
						switch (i.FullPath()) {
							case "Utilities.Mediator.Handlers.IMirrorRequestHandler": {
								var input = i.TypeArguments[0];
								var inputContract = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IMirrorRequest" && x.TypeArguments.Length is 3);
								if (inputContract is null) {
									break;
								}

								var messageContractArgs = inputContract.TypeArguments;
								var originInput = messageContractArgs[0];
								var originOutput = messageContractArgs[1];
								var output = messageContractArgs[2];
								var m = new WrappableMirrorMessage(MessageType.Request, originInput, input, originOutput, output);
								h.Mirror.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Handlers.IRequestHandler": {
								var input = i.TypeArguments[0];
								var output = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IRequest" && x.TypeArguments.Length is 1)
								  ?.TypeArguments[0];
								if (output is null) {
									break;
								}

								var m = new WrappableMessage(MessageType.Request, input, output);
								h.Common.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Scope.IScopedRequestHandler": {
								var input = i.TypeArguments[0];
								var output = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IRequest" && x.TypeArguments.Length is 1)
								  ?.TypeArguments[0];
								if (output is null) {
									break;
								}

								var m = new WrappableMessage(MessageType.Request, input, output, true);
								h.Common.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Handlers.IMirrorCommandHandler": {
								var input = i.TypeArguments[0];
								var inputContract = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IMirrorCommand" && x.TypeArguments.Length is 3);
								if (inputContract is null) {
									break;
								}

								var messageContractArgs = inputContract.TypeArguments;
								var originInput = messageContractArgs[0];
								var originOutput = messageContractArgs[1];
								var output = messageContractArgs[2];
								var m = new WrappableMirrorMessage(MessageType.Command, originInput, input, originOutput, output);
								h.Mirror.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Handlers.ICommandHandler": {
								var input = i.TypeArguments[0];
								var output = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.ICommand" && x.TypeArguments.Length is 1)
								  ?.TypeArguments[0];
								if (output is null) {
									break;
								}

								var m = new WrappableMessage(MessageType.Command, input, output);
								h.Common.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Scope.IScopedCommandHandler": {
								var input = i.TypeArguments[0];
								var output = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.ICommand" && x.TypeArguments.Length is 1)
								  ?.TypeArguments[0];
								if (output is null) {
									break;
								}

								var m = new WrappableMessage(MessageType.Command, input, output, true);
								h.Common.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Handlers.IMirrorQueryHandler": {
								var input = i.TypeArguments[0];
								var inputContract = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IMirrorQuery" && x.TypeArguments.Length is 3);
								if (inputContract is null) {
									break;
								}

								var messageContractArgs = inputContract.TypeArguments;
								var originInput = messageContractArgs[0];
								var originOutput = messageContractArgs[1];
								var output = messageContractArgs[2];
								var m = new WrappableMirrorMessage(MessageType.Query, originInput, input, originOutput, output);
								h.Mirror.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Handlers.IQueryHandler": {
								var input = i.TypeArguments[0];
								var output = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IQuery" && x.TypeArguments.Length is 1)
								  ?.TypeArguments[0];
								if (output is null) {
									break;
								}

								var m = new WrappableMessage(MessageType.Query, input, output);
								h.Common.Messages.Add(m);
								break;
							}
							case "Utilities.Mediator.Scope.IScopedQueryHandler": {
								var input = i.TypeArguments[0];
								var output = input.Interfaces
								   .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IQuery" && x.TypeArguments.Length is 1)
								  ?.TypeArguments[0];
								if (output is null) {
									break;
								}

								var m = new WrappableMessage(MessageType.Query, input, output, true);
								h.Common.Messages.Add(m);
								break;
							}
						}
					}

					return h.Any() ? h : default;
				}
			)
		   .Where(x => x != default);

		context.RegisterSourceOutput(
			handlersToExtendWithPartials.Collect(),
			static (context, handlers) => {
				if (handlers.IsDefaultOrEmpty) {
					return;
				}

				foreach (var group in handlers.GroupBy(h => h.Type.ContainingNamespace, SymbolEqualityComparer.Default)) {
					context.CancellationToken.ThrowIfCancellationRequested();
					using var writer = new IndentedTextWriter(new StringWriter(), "	");

					writer.WriteLines(
						"// <auto-generated />",
						"#pragma warning disable CS8019",
						"#pragma warning disable CS0105",
						"#nullable enable",
						null,
						"using Utilities.Mediator.Handlers;",
						"using Utilities.Mediator.Messages;",
						"using Utilities.Mediator.Scope;",
						null
					);

					var ns = (INamespaceSymbol)group.Key!;
					writer.WriteLine($"namespace {ns} {{");

					foreach (var h in group) {
						var ending = h.Type.IsRecord ? ";" : " {}";
						if (h.Mirror.Any()) {
							if (h.Mirror.Messages.Count > 1) {
								var interfaces = h.Mirror.Messages.Select(x => x.HandlerInterfaceTypeText);
								writer.WriteLine($"partial {h.Type.DeclarationString()} {h.Type.Name} :");
								writer.WriteLines(interfaces.Select((i, n) => $"{(n > 0 ? ',' : null)}{i}"));
								writer.WriteLine(ending);
							} else {
								writer.WriteLine(
									$"partial {h.Type.DeclarationString()} {h.Type.Name} : {h.Mirror.Messages[0].HandlerInterfaceTypeText}{ending}"
								);
							}
						}

						if (h.Common.Any()) {
							if (h.Common.Messages.Count > 1) {
								var interfaces = h.Common.Messages.Select(x => x.HandlerInterfaceTypeText);
								writer.WriteLine($"partial {h.Type.DeclarationString()} {h.Type.Name} :");
								writer.WriteLines(interfaces.Select((i, n) => $"{(n > 0 ? ',' : null)}{i}"));
								writer.WriteLine(ending);
							} else {
								writer.WriteLine(
									$"partial {h.Type.DeclarationString()} {h.Type.Name} : {h.Common.Messages[0].HandlerInterfaceTypeText}{ending}"
								);
							}
						}
					}

					writer.WriteLine("}");

					var sourceCode = writer.InnerWriter.ToString()!;
					var source = SourceText.From(sourceCode, Encoding.UTF8);
					context.AddSource($"{ns.MinimalName()}.g.cs", source);
				}
			}
		);

		#endregion

		#region Cover Messages

		var coverMessages = context.SyntaxProvider.CreateSyntaxProvider(
				static (node, ct) => {
					if (node is TypeDeclarationSyntax {
							TypeParameterList: null,
							BaseList.Types.Count: > 0,
							RawKind: (int)SyntaxKind.StructDeclaration or (int)SyntaxKind.RecordStructDeclaration,
							Keyword.Text: "record" or "struct"
						} td) {
						foreach (var bt in td.BaseList.Types) {
							if (bt.Type is GenericNameSyntax {
									Identifier.Text: "ICoverRequest" or "ICoverCommand" or "ICoverQuery",
									TypeArgumentList.Arguments.Count: 1
								}) {
								return true;
							}
						}
					}

					return false;
				},
				static (context, ct) => {
					var syntax = (TypeDeclarationSyntax)context.Node;

					const string coverRequest = "Utilities.Mediator.Messages.ICoverRequest";
					const string coverCommand = "Utilities.Mediator.Messages.ICoverCommand";
					const string coverQuery = "Utilities.Mediator.Messages.ICoverQuery";

					if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } symbol) {
						return default;
					}

					var contract = symbol.Interfaces.FirstOrDefault(x => x.FullPath() is coverRequest or coverCommand or coverQuery);
					if (contract is null) {
						return default;
					}

					var coveredMessage = (INamedTypeSymbol)contract.TypeArguments[0];

					return (Cover: symbol, Message: coveredMessage);
				}
			)
		   .Where(x => x != default);

		context.RegisterSourceOutput(
			coverMessages.Collect(),
			static (context, tuples) => {
				if (tuples.IsDefaultOrEmpty) {
					return;
				}

				using var writer = new IndentedTextWriter(new StringWriter(), "	");

				writer.WriteLines(
					"// <auto-generated />",
					"#pragma warning disable CS8019",
					"#pragma warning disable CS0105",
					"#nullable enable",
					null,
					"using Utilities.Mediator.Messages;",
					null
				);

				foreach (var group in tuples.GroupBy(x => x.Message.ContainingNamespace, SymbolEqualityComparer.Default)) {
					var ns = (INamespaceSymbol)group.Key!;
					writer.WriteLine($"namespace {ns.ToDisplayString()} {{");

					foreach (var groupByMessage in group.GroupBy(x => x.Message, SymbolEqualityComparer.Default)) {
						var m = (INamedTypeSymbol)groupByMessage.Key!;
						var ending = m.IsRecord ? ";" : " {}";
						var covers = groupByMessage.AsArray().AsSpan();

						if (covers.Length is 1) {
							var (c, _) = covers[0];
							writer.WriteLine(
								$"partial {m.DeclarationString()} {m.Name} : ICoveredBy<{c.GlobalName()}>{ending}"
							);
						} else {
							writer.WriteLine($"partial {m.DeclarationString()} {m.Name} :");
							writer.Indent++;
							for (var i = 0; i < covers.Length; i++) {
								var (c, _) = covers[i];
								writer.WriteLine($"{(i > 0 ? ',' : null)} ICoveredBy<{c.GlobalName()}>{(i < covers.Length - 1 ? null : ending)}");
							}

							writer.Indent--;
						}
					}

					writer.WriteLine("}");
				}


				var sourceCode = writer.InnerWriter.ToString()!;

				context.AddSource("Mediator_Cover_Messages.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
			}
		);

		#endregion
	}
}
