using Perf.SourceGeneration.Mediator.Templates;

namespace Perf.SourceGeneration.Mediator;

[Generator]
internal sealed partial class MediatorGenerator : IIncrementalGenerator {
    private const string MediatorClassAssemblyAttributeName = "Utilities.Mediator.Attributes.MediatorClassAttribute";
    private const string MediatorInterfaceFullName = "Utilities.Mediator.IMediator";

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

                            var staticExecution = attributeData.NamedArguments.FirstOrDefault(x => x.Key is "StaticExecution").Value.As<bool>();

                            var result = new Assembly(mediatorType) {
                                StaticExecution = staticExecution
                            };
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
                            // Common handlers
                            if (bt.Type is GenericNameSyntax {
                                TypeArgumentList.Arguments.Count: 1 or 2,
                                Identifier.Text: "IRequestHandler" or "ICommandHandler" or "IQueryHandler" or "INotificationHandler"
                            }) {
                                return true;
                            }

                            // Mirror handlers
                            if (bt.Type is GenericNameSyntax {
                                TypeArgumentList.Arguments.Count: 1 or 4,
                                Identifier.Text: "IMirrorRequestHandler" or "IMirrorCommandHandler" or "IMirrorQueryHandler"
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
                        case "Utilities.Mediator.Handlers.IRequestHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IRequest" && x.TypeArguments.Length is 1);
                            if (inputContract is null) {
                                break;
                            }

                            var output = inputContract.TypeArguments[0];
                            var m = new WrappableMessage(MessageType.Request, input, output);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IRequestHandler" when args.Length is 2: {
                            var input = args[0];
                            var output = args[1];
                            var m = new WrappableMessage(MessageType.Request, input, output);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IMirrorRequestHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputFullContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IMirrorRequest" && x.TypeArguments.Length is 3);
                            if (inputFullContract is null) {
                                break;
                            }

                            var originInput = inputFullContract.TypeArguments[0];
                            var originOutput = inputFullContract.TypeArguments[1];
                            var output = inputFullContract.TypeArguments[2];
                            var m = new WrappableMirrorMessage(MessageType.Request, originInput, input, originOutput, output);
                            h.Mirror.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IMirrorRequestHandler" when args.Length is 4: {
                            var originInput = args[0];
                            var originOutput = args[1];
                            var input = args[2];
                            var output = args[3];
                            var m = new WrappableMirrorMessage(MessageType.Request, originInput, input, originOutput, output);
                            h.Mirror.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.ICommandHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.ICommand" && x.TypeArguments.Length is 1);
                            if (inputContract is null) {
                                break;
                            }

                            var output = inputContract.TypeArguments[0];
                            var m = new WrappableMessage(MessageType.Command, input, output);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.ICommandHandler" when args.Length is 2: {
                            var input = args[0];
                            var output = args[1];
                            var m = new WrappableMessage(MessageType.Command, input, output);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IMirrorCommandHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputFullContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IMirrorCommand" && x.TypeArguments.Length is 3);
                            if (inputFullContract is null) {
                                break;
                            }

                            var originInput = inputFullContract.TypeArguments[0];
                            var originOutput = inputFullContract.TypeArguments[1];
                            var output = inputFullContract.TypeArguments[2];
                            var m = new WrappableMirrorMessage(MessageType.Command, originInput, input, originOutput, output);
                            h.Mirror.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IMirrorCommandHandler" when args.Length is 4: {
                            var originInput = args[0];
                            var originOutput = args[1];
                            var input = args[2];
                            var output = args[3];
                            var m = new WrappableMirrorMessage(MessageType.Command, originInput, input, originOutput, output);
                            h.Mirror.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IQueryHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IQuery" && x.TypeArguments.Length is 1);
                            if (inputContract is null) {
                                break;
                            }

                            var output = inputContract.TypeArguments[0];
                            var m = new WrappableMessage(MessageType.Query, input, output);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IQueryHandler" when args.Length is 2: {
                            var input = args[0];
                            var output = args[1];
                            var m = new WrappableMessage(MessageType.Query, input, output);
                            h.Common.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IMirrorQueryHandler" when args.Length is 1: {
                            var input = args[0];
                            var inputFullContract = input.Interfaces
                               .FirstOrDefault(x => x.FullPath() is "Utilities.Mediator.Messages.IMirrorQuery" && x.TypeArguments.Length is 3);
                            if (inputFullContract is null) {
                                break;
                            }

                            var originInput = inputFullContract.TypeArguments[0];
                            var originOutput = inputFullContract.TypeArguments[1];
                            var output = inputFullContract.TypeArguments[2];
                            var m = new WrappableMirrorMessage(MessageType.Query, originInput, input, originOutput, output);
                            h.Mirror.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.IMirrorQueryHandler" when args.Length is 4: {
                            var originInput = args[0];
                            var originOutput = args[1];
                            var input = args[2];
                            var output = args[3];
                            var m = new WrappableMirrorMessage(MessageType.Query, originInput, input, originOutput, output);
                            h.Mirror.Messages.Add(m);
                            break;
                        }
                        case "Utilities.Mediator.Handlers.INotificationHandler" when args.Length is 1: {
                            var input = args[0];
                            var m = new Notification(input);
                            h.Common.Notifications.Add(m);
                            break;
                        }
                        }
                    }

                    return h.Any() ? h : default;
                }
            )
           .Where(x => x != default);

        context.RegisterSourceOutput(
            assemblyAttribute.Combine(handlersForMediator.Collect()),
            static (context, tuple) => {
                var (assembly, handlers) = tuple;
                var ct = context.CancellationToken;

                if (handlers.Any(x => x.Common.Notifications.Any())) {
                    assembly = assembly with {
                        AnyNotifications = true
                    };
                }

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
                    "using Microsoft.Extensions.DependencyInjection;",
                    "using Microsoft.Extensions.DependencyInjection.Extensions;",
                    "using Microsoft.Extensions.Hosting;",
                    "using Microsoft.Extensions.Logging;",
                    "using System.Threading.Channels;",
                    "using System.Runtime.CompilerServices;",
                    null
                );

                ct.ThrowIfCancellationRequested();
                Generator.WriteRegistrationExtension(writer, assembly, handlers);
                Generator.WriteMediatorClass(writer, assembly, handlers);
                if (assembly.AnyNotifications) {
                    Generator.WriteNotificationBus(writer, assembly, handlers);
                }

                if (assembly.MediatorInterface is not null && assembly.MediatorInterfacePartial) {
                    Generator.WriteMediatorInterfaceTypedMethods(writer, assembly, handlers);
                }

                ct.ThrowIfCancellationRequested();
                var mediatorSourceCode = writer.InnerWriter.ToString()!;
                var mediatorSource = SourceText.From(mediatorSourceCode, Encoding.UTF8);
                ct.ThrowIfCancellationRequested();
                context.AddSource($"{assembly.Mediator.Name}.g.cs", mediatorSource);

                if (assembly.StaticExecution) {
                    ct.ThrowIfCancellationRequested();
                    var staticExecutionSourceCode = Generator.GenerateStaticExecutionMethods(assembly, handlers);
                    context.AddSource($"{assembly.Mediator.Name}.Static.g.cs", SourceText.From(staticExecutionSourceCode, Encoding.UTF8));
                }
            }
        );
    }
}
