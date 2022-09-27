using Mediator.Handlers;
using Mediator.Pipelines;

namespace Mediator.Wrappers;

public sealed class MirrorRequestClassHandlerWrapper<TOriginRequest, TOriginResponse, TMirrorRequest, TMirrorResponse>
	where TOriginRequest : IRequest<TOriginResponse>
	where TMirrorRequest : class, IMirrorRequest<TOriginRequest, TOriginResponse, TMirrorResponse> {
	private readonly CommonHandlerDelegate<TMirrorRequest, TMirrorResponse> rootHandler;

	public MirrorRequestClassHandlerWrapper(
		IMirrorRequestHandler<TOriginRequest, TOriginResponse, TMirrorRequest, TMirrorResponse> concreteHandler,
		CommonHandlerDelegate<TOriginRequest, TOriginResponse> originHandler,
		IEnumerable<IPipelineBehavior<TMirrorRequest, TMirrorResponse>> pipelineBehaviours
	) {
		CommonHandlerDelegate<TMirrorRequest, TMirrorResponse> handler = (message, ct) => concreteHandler.Handle(message, originHandler, ct);

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TMirrorResponse> Handle(TMirrorRequest request, CancellationToken ct) => rootHandler.Invoke(request, ct);
}

public sealed class MirrorRequestStructHandlerWrapper<TOriginRequest, TOriginResponse, TMirrorRequest, TMirrorResponse>
	where TOriginRequest : IRequest<TOriginResponse>
	where TMirrorRequest : struct, IMirrorRequest<TOriginRequest, TOriginResponse, TMirrorResponse> {
	private readonly CommonHandlerDelegate<TMirrorRequest, TMirrorResponse> rootHandler;

	public MirrorRequestStructHandlerWrapper(
		IMirrorRequestHandler<TOriginRequest, TOriginResponse, TMirrorRequest, TMirrorResponse> concreteHandler,
		CommonHandlerDelegate<TOriginRequest, TOriginResponse> originHandler,
		IEnumerable<IPipelineBehavior<TMirrorRequest, TMirrorResponse>> pipelineBehaviours
	) {
		CommonHandlerDelegate<TMirrorRequest, TMirrorResponse> handler = (message, ct) => concreteHandler.Handle(message, originHandler, ct);

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TMirrorResponse> Handle(TMirrorRequest request, CancellationToken ct) => rootHandler.Invoke(request, ct);
}

public sealed class MirrorCommandClassHandlerWrapper<TOriginCommand, TOriginResponse, TMirrorCommand, TMirrorResponse>
	where TOriginCommand : ICommand<TOriginResponse>
	where TMirrorCommand : class, IMirrorCommand<TOriginCommand, TOriginResponse, TMirrorResponse> {
	private readonly CommonHandlerDelegate<TMirrorCommand, TMirrorResponse> rootHandler;

	public MirrorCommandClassHandlerWrapper(
		IMirrorCommandHandler<TOriginCommand, TOriginResponse, TMirrorCommand, TMirrorResponse> concreteHandler,
		CommonHandlerDelegate<TOriginCommand, TOriginResponse> originHandler,
		IEnumerable<IPipelineBehavior<TMirrorCommand, TMirrorResponse>> pipelineBehaviours
	) {
		CommonHandlerDelegate<TMirrorCommand, TMirrorResponse> handler = (message, ct) => concreteHandler.Handle(message, originHandler, ct);

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TMirrorResponse> Handle(TMirrorCommand command, CancellationToken ct) => rootHandler.Invoke(command, ct);
}

public sealed class MirrorCommandStructHandlerWrapper<TOriginCommand, TOriginResponse, TMirrorCommand, TMirrorResponse>
	where TOriginCommand : ICommand<TOriginResponse>
	where TMirrorCommand : struct, IMirrorCommand<TOriginCommand, TOriginResponse, TMirrorResponse> {
	private readonly CommonHandlerDelegate<TMirrorCommand, TMirrorResponse> rootHandler;

	public MirrorCommandStructHandlerWrapper(
		IMirrorCommandHandler<TOriginCommand, TOriginResponse, TMirrorCommand, TMirrorResponse> concreteHandler,
		CommonHandlerDelegate<TOriginCommand, TOriginResponse> originHandler,
		IEnumerable<IPipelineBehavior<TMirrorCommand, TMirrorResponse>> pipelineBehaviours
	) {
		CommonHandlerDelegate<TMirrorCommand, TMirrorResponse> handler = (message, ct) => concreteHandler.Handle(message, originHandler, ct);

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TMirrorResponse> Handle(TMirrorCommand command, CancellationToken ct) => rootHandler.Invoke(command, ct);
}

public sealed class MirrorQueryClassHandlerWrapper<TOriginQuery, TOriginResponse, TMirrorQuery, TMirrorResponse>
	where TOriginQuery : IQuery<TOriginResponse>
	where TMirrorQuery : class, IMirrorQuery<TOriginQuery, TOriginResponse, TMirrorResponse> {
	private readonly CommonHandlerDelegate<TMirrorQuery, TMirrorResponse> rootHandler;

	public MirrorQueryClassHandlerWrapper(
		IMirrorQueryHandler<TOriginQuery, TOriginResponse, TMirrorQuery, TMirrorResponse> concreteHandler,
		CommonHandlerDelegate<TOriginQuery, TOriginResponse> originHandler,
		IEnumerable<IPipelineBehavior<TMirrorQuery, TMirrorResponse>> pipelineBehaviours
	) {
		CommonHandlerDelegate<TMirrorQuery, TMirrorResponse> handler = (message, ct) => concreteHandler.Handle(message, originHandler, ct);

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TMirrorResponse> Handle(TMirrorQuery query, CancellationToken ct) => rootHandler.Invoke(query, ct);
}

public sealed class MirrorQueryStructHandlerWrapper<TOriginQuery, TOriginResponse, TMirrorQuery, TMirrorResponse>
	where TOriginQuery : IQuery<TOriginResponse>
	where TMirrorQuery : class, IMirrorQuery<TOriginQuery, TOriginResponse, TMirrorResponse> {
	private readonly CommonHandlerDelegate<TMirrorQuery, TMirrorResponse> rootHandler;

	public MirrorQueryStructHandlerWrapper(
		IMirrorQueryHandler<TOriginQuery, TOriginResponse, TMirrorQuery, TMirrorResponse> concreteHandler,
		CommonHandlerDelegate<TOriginQuery, TOriginResponse> originHandler,
		IEnumerable<IPipelineBehavior<TMirrorQuery, TMirrorResponse>> pipelineBehaviours
	) {
		CommonHandlerDelegate<TMirrorQuery, TMirrorResponse> handler = (message, ct) => concreteHandler.Handle(message, originHandler, ct);

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TMirrorResponse> Handle(TMirrorQuery query, CancellationToken ct) => rootHandler.Invoke(query, ct);
}
