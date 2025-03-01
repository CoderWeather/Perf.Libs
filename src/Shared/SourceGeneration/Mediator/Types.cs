namespace Perf.SourceGeneration.Mediator;

internal readonly record struct Assembly(INamedTypeSymbol Mediator) {
    public INamedTypeSymbol? MediatorInterface { get; init; } = null;
    public bool MediatorInterfacePartial { get; init; } = false;
    public bool IsScoped { get; init; } = false;
    public bool StaticExecution { get; init; }
    public bool AnyNotifications { get; init; }
}

internal readonly record struct Handler(INamedTypeSymbol Type) {
    public readonly CommonContracts Common = new();
    public readonly MirrorMessages Mirror = new();
    public bool Any() => Common.Any() || Mirror.Any();
}

internal enum MessageType {
    Request,
    Command,
    Query,
    Notification
}

#region Common

internal readonly record struct CommonContracts {
    public CommonContracts() { }
    public bool Any() => Messages.Any() || Notifications.Any();
    public readonly List<WrappableMessage> Messages = new(0);
    public readonly List<Notification> Notifications = new(0);
}

internal interface IMessage<T> where T : struct {
    ITypeSymbol Input { get; }
}

internal readonly record struct WrappableMessage(
    MessageType Type,
    ITypeSymbol Input,
    ITypeSymbol Output,
    bool IsScoped = false
) : IMessage<WrappableMessage> {
    public string HandlerInterfaceTypeText =>
        Type switch {
            MessageType.Request => IsScoped ? $"IScopedRequestHandler<{Input}, {Output}>" : $"IRequestHandler<{Input}, {Output}>",
            MessageType.Command => IsScoped ? $"IScopedCommandHandler<{Input}, {Output}>" : $"ICommandHandler<{Input}, {Output}>",
            MessageType.Query   => IsScoped ? $"IScopedQueryHandler<{Input}, {Output}>" : $"IQueryHandler<{Input}, {Output}>",
            _                   => throw new("Out of range is IMPOSSIBLE here")
        };

    public string InputTypeText =>
        Type switch {
            MessageType.Request when IsScoped => $"ScopedRequest<{Input}, {Output}>",
            MessageType.Request               => Input.GlobalName(),
            MessageType.Command when IsScoped => $"ScopedCommand<{Input}, {Output}>",
            MessageType.Command               => Input.GlobalName(),
            MessageType.Query when IsScoped   => $"ScopedQuery<{Input}, {Output}>",
            MessageType.Query                 => Input.GlobalName(),
            _                                 => throw new("Out of range is IMPOSSIBLE here")
        };

    public string WrapperTypeText =>
        Type switch {
            MessageType.Request => Input.IsValueType || IsScoped
                ? $"RequestStructHandlerWrapper<{InputTypeText}, {Output}>"
                : $"RequestClassHandlerWrapper<{InputTypeText}, {Output}>",
            MessageType.Command => Input.IsValueType || IsScoped
                ? $"CommandStructHandlerWrapper<{InputTypeText}, {Output}>"
                : $"CommandClassHandlerWrapper<{InputTypeText}, {Output}>",
            MessageType.Query => Input.IsValueType || IsScoped
                ? $"QueryStructHandlerWrapper<{InputTypeText}, {Output}>"
                : $"QueryClassHandlerWrapper<{InputTypeText}, {Output}>",
            _ => throw new("Out of range is IMPOSSIBLE here")
        };

    public string PipelineTypeText => $"IPipelineBehavior<{InputTypeText}, {Output}>";
}

internal readonly record struct Notification(ITypeSymbol Input) : IMessage<Notification>;

#endregion

#region Mirrors

internal readonly record struct MirrorMessages {
    public MirrorMessages() { }
    public bool Any() => Messages.Any();
    public readonly List<WrappableMirrorMessage> Messages = new(0);
}

internal interface IMirrorMessage<T> where T : struct {
    ITypeSymbol OriginInput { get; }
    ITypeSymbol Input { get; }
}

internal readonly record struct WrappableMirrorMessage(
    MessageType Type,
    ITypeSymbol OriginInput,
    ITypeSymbol Input,
    ITypeSymbol OriginOutput,
    ITypeSymbol Output
) : IMirrorMessage<WrappableMessage> {
    public readonly WrappableMessage Origin = new(Type, OriginInput, OriginOutput);

    public string HandlerInterfaceTypeText =>
        Type switch {
            MessageType.Request =>
                $"IMirrorRequestHandler<{OriginInput.GlobalName()}, {OriginOutput.GlobalName()}, {Input.GlobalName()}, {Output.GlobalName()}>",
            MessageType.Command =>
                $"IMirrorCommandHandler<{OriginInput.GlobalName()}, {OriginOutput.GlobalName()}, {Input.GlobalName()}, {Output.GlobalName()}>",
            MessageType.Query =>
                $"IMirrorQueryHandler<{OriginInput.GlobalName()}, {OriginOutput.GlobalName()}, {Input.GlobalName()}, {Output.GlobalName()}>",
            _ => throw new("Out of range is IMPOSSIBLE here")
        };

    public string MessageInterfaceTypeText =>
        Type switch {
            MessageType.Request => $"IMirrorRequest<{OriginInput.GlobalName()}, {OriginOutput.GlobalName()}, {Output.GlobalName()}>",
            MessageType.Command => $"IMirrorCommand<{OriginInput.GlobalName()}, {OriginOutput.GlobalName()}, {Output.GlobalName()}>",
            MessageType.Query   => $"IMirrorQuery<{OriginInput.GlobalName()}, {OriginOutput.GlobalName()}, {Output.GlobalName()}>",
            _                   => throw new("Out of range is IMPOSSIBLE here")
        };

    public string WrapperTypeText =>
        Type switch {
            MessageType.Request => Input.IsValueType
                ? $"MirrorRequestStructHandlerWrapper<{OriginInput}, {OriginOutput}, {Input}, {Output}>"
                : $"MirrorRequestClassHandlerWrapper<{OriginInput}, {OriginOutput}, {Input}, {Output}>",
            MessageType.Command => Input.IsValueType
                ? $"MirrorCommandStructHandlerWrapper<{OriginInput}, {OriginOutput}, {Input}, {Output}>"
                : $"MirrorCommandClassHandlerWrapper<{OriginInput}, {OriginOutput}, {Input}, {Output}>",
            MessageType.Query => Input.IsValueType
                ? $"MirrorQueryStructHandlerWrapper<{OriginInput}, {OriginOutput}, {Input}, {Output}>"
                : $"MirrorQueryClassHandlerWrapper<{OriginInput}, {OriginOutput}, {Input}, {Output}>",
            _ => throw new("Out of range is IMPOSSIBLE here")
        };

    public string OriginWrapperTypeText => Origin.WrapperTypeText;

    public string PipelineTypeText => $"IPipelineBehavior<{Input}, {Output}>";
}

#endregion

internal static class TypeExtensions {
    public static IEnumerable<string> GetNamespaces(this WrappableMessage message) {
        yield return message.Input.ContainingNamespace.ToDisplayString();
        yield return message.Output.ContainingNamespace.ToDisplayString();
    }

    public static IEnumerable<string> GetNamespaces(this WrappableMirrorMessage message) {
        yield return message.Input.ContainingNamespace.ToDisplayString();
        yield return message.Output.ContainingNamespace.ToDisplayString();
        yield return message.OriginInput.ContainingNamespace.ToDisplayString();
        yield return message.OriginOutput.ContainingNamespace.ToDisplayString();
    }
}
