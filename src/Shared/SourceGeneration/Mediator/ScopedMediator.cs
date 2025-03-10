using Perf.SourceGeneration.Mediator.Templates;

namespace Perf.SourceGeneration.Mediator;

[Generator]
internal sealed class ScopedMediatorGenerator : IIncrementalGenerator {
    private const string MediatorClassAssemblyAttributeName = "Utilities.Mediator.Attributes.ScopedMediatorClassAttribute";
    private const string MediatorInterfaceFullName = "Utilities.Mediator.Scope.IScopedMediator";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var assemblyAttribute = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, ct) => node is AttributeListSyntax {
                    Target.Identifier.RawKind: (int)SyntaxKind.AssemblyKeyword
                },
                static (context, ct) => {
                    var syntax = (AttributeListSyntax)context.Node;
                    foreach (var attributeSyntax in syntax.Attributes) {
                        if (context.SemanticModel.GetSymbolInfo(attributeSyntax, ct).Symbol is not IMethodSymbol attributeConstructor) {
                            continue;
                        }

                        var attributeUsedType = attributeConstructor.ContainingType.OriginalDefinition;
                        if (attributeUsedType.FullPath() is MediatorClassAssemblyAttributeName) {
                            var attributeData = context.SemanticModel.Compilation.Assembly.GetAttribute(MediatorClassAssemblyAttributeName);
                            var mediatorType = attributeData.ConstructorArguments[0].As<INamedTypeSymbol>()!;
                            if (mediatorType is not { IsStatic: false, IsGenericType: false, IsAbstract: false, TypeKind: TypeKind.Class }) {
                                return default;
                            }

                            if (mediatorType.AllInterfaces.Any(x => x.FullPath() is MediatorInterfaceFullName) is false) {
                                return default;
                            }

                            var result = new Assembly(mediatorType);
                            var tryGetOwnInterface = mediatorType.Interfaces
                               .FirstOrDefault(x => x.Interfaces.Any(y => y.FullPath() is MediatorInterfaceFullName));
                            if (tryGetOwnInterface is not null) {
                                if (tryGetOwnInterface.DeclaringSyntaxReferences[0].GetSyntax() is InterfaceDeclarationSyntax ids) {
                                    result = result with {
                                        MediatorInterface = tryGetOwnInterface,
                                        MediatorInterfacePartial = ids.Modifiers.Any(SyntaxKind.PartialKeyword)
                                    };
                                } else {
                                    result = result with {
                                        MediatorInterface = tryGetOwnInterface
                                    };
                                }
                            }

                            return result;
                        }
                    }

                    return default;
                }
            )
           .Where(x => x != default);

        var handlersForMediator = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, ct) => {
                    if (node is TypeDeclarationSyntax {
                            TypeParameterList: null,
                            BaseList.Types.Count: > 0,
                            RawKind: (int)SyntaxKind.ClassDeclaration or (int)SyntaxKind.RecordDeclaration
                        } t
                     && t.Modifiers.Any(SyntaxKind.SealedKeyword)) {
                        foreach (var bt in t.BaseList.Types) {
                            ct.ThrowIfCancellationRequested();
                            if (bt.Type is GenericNameSyntax {
                                TypeArgumentList.Arguments.Count: 1 or 2,
                                Identifier.Text: "IScopedRequestHandler" or "IScopedCommandHandler" or "IScopedQueryHandler"
                            }) {
                                return true;
                            }
                        }
                    }

                    return false;
                },
                static (context, ct) => {
                    var syntax = (TypeDeclarationSyntax)context.Node;
                    if (context.SemanticModel.GetDeclaredSymbol(syntax, ct) is not { } symbol) {
                        return default;
                    }

                    var h = new Handler(symbol);
                    foreach (var i in symbol.Interfaces) {
                        ct.ThrowIfCancellationRequested();

                        var args = i.TypeArguments;
                        switch (i.FullPath()) {
                        case "Utilities.Mediator.Scope.IScopedRequestHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IRequest" && x.TypeArguments.Length is 1);
                            if (inputContract is null) {
                                break;
                            }

                            var output = inputContract.TypeArguments[0];
                            var m = new WrappableMessage(MessageType.Request, input, output, true);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Scope.IScopedRequestHandler" when args.Length is 2: {
                            var input = args[0];
                            var output = args[1];
                            var m = new WrappableMessage(MessageType.Request, input, output, true);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Scope.IScopedCommandHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.ICommand" && x.TypeArguments.Length is 1);
                            if (inputContract is null) {
                                break;
                            }

                            var output = inputContract.TypeArguments[0];
                            var m = new WrappableMessage(MessageType.Command, input, output, true);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Scope.IScopedCommandHandler" when args.Length is 2: {
                            var input = args[0];
                            var output = args[1];
                            var m = new WrappableMessage(MessageType.Command, input, output, true);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Scope.IScopedQueryHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IQuery" && x.TypeArguments.Length is 1);
                            if (inputContract is null) {
                                break;
                            }

                            var output = inputContract.TypeArguments[0];
                            var m = new WrappableMessage(MessageType.Query, input, output, true);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Scope.IScopedQueryHandler" when args.Length is 2: {
                            var input = args[0];
                            var output = args[1];
                            var m = new WrappableMessage(MessageType.Query, input, output, true);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        }
                    }

                    return h;
                }
            )
           .Where(x => x != default);

        context.RegisterSourceOutput(
            assemblyAttribute.Combine(handlersForMediator.Collect()),
            static (context, tuple) => {
                var (assembly, handlers) = tuple;
                var ct = context.CancellationToken;

                ct.ThrowIfCancellationRequested();
                using var writer = new IndentedTextWriter(new StringWriter(), "	");

                writer.WriteLines(
                    "// <auto-generated />",
                    "#pragma warning disable CS8019",
                    "#pragma warning disable CS0105",
                    "#nullable enable",
                    null,
                    $"namespace {assembly.Mediator.ContainingNamespace};",
                    null,
                    "using Utilities.Mediator;",
                    "using Utilities.Mediator.Attributes;",
                    "using Utilities.Mediator.Handlers;",
                    "using Utilities.Mediator.Messages;",
                    "using Utilities.Mediator.Wrappers;",
                    "using Utilities.Mediator.Pipelines;",
                    "using Utilities.Mediator.Scope;",
                    "using Microsoft.Extensions.DependencyInjection;",
                    "using Microsoft.Extensions.DependencyInjection.Extensions;",
                    "using Microsoft.Extensions.Hosting;",
                    "using Microsoft.Extensions.Logging;",
                    "using System.Threading.Channels;",
                    "using System.Runtime.CompilerServices;",
                    null
                );

                ct.ThrowIfCancellationRequested();
                Generator.WriteScopeRegistrationExtension(writer, assembly, handlers);
                Generator.WriteScopedMediatorClass(writer, assembly, handlers);
                if (assembly.MediatorInterface is not null && assembly.MediatorInterfacePartial) {
                    Generator.WriteMediatorInterfaceTypedMethods(writer, assembly, handlers);
                }

                ct.ThrowIfCancellationRequested();
                var mediatorSourceCode = writer.InnerWriter.ToString()!;
                var mediatorSource = SourceText.From(mediatorSourceCode, Encoding.UTF8);
                ct.ThrowIfCancellationRequested();
                context.AddSource($"{assembly.Mediator.Name}.g.cs", mediatorSource);
            }
        );
    }
}
