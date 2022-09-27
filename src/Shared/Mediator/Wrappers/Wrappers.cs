using Mediator.Handlers;
using Mediator.Pipelines;

namespace Mediator.Wrappers;

public sealed class RequestClassHandlerWrapper<TRequest, TResponse>
	where TRequest : class, IRequest<TResponse> {
	private readonly CommonHandlerDelegate<TRequest, TResponse> rootHandler;

	public RequestClassHandlerWrapper(
		IRequestHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (CommonHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class RequestStructHandlerWrapper<TRequest, TResponse>
	where TRequest : struct, IRequest<TResponse> {
	private readonly CommonHandlerDelegate<TRequest, TResponse> rootHandler;

	public RequestStructHandlerWrapper(
		IRequestHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (CommonHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class StreamRequestClassHandlerWrapper<TRequest, TResponse>
	where TRequest : class, IStreamRequest<TResponse> {
	private readonly StreamHandlerDelegate<TRequest, TResponse> rootHandler;

	public StreamRequestClassHandlerWrapper(
		IStreamRequestHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (StreamHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class StreamRequestStructHandlerWrapper<TRequest, TResponse>
	where TRequest : struct, IStreamRequest<TResponse> {
	private readonly StreamHandlerDelegate<TRequest, TResponse> rootHandler;

	public StreamRequestStructHandlerWrapper(
		IStreamRequestHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (StreamHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class CommandClassHandlerWrapper<TRequest, TResponse>
	where TRequest : class, ICommand<TResponse> {
	private readonly CommonHandlerDelegate<TRequest, TResponse> rootHandler;

	public CommandClassHandlerWrapper(
		ICommandHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (CommonHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class CommandStructHandlerWrapper<TRequest, TResponse>
	where TRequest : struct, ICommand<TResponse> {
	private readonly CommonHandlerDelegate<TRequest, TResponse> rootHandler;

	public CommandStructHandlerWrapper(
		ICommandHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (CommonHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class StreamCommandClassHandlerWrapper<TRequest, TResponse>
	where TRequest : class, IStreamCommand<TResponse> {
	private readonly StreamHandlerDelegate<TRequest, TResponse> rootHandler;

	public StreamCommandClassHandlerWrapper(
		IStreamCommandHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (StreamHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class StreamCommandStructHandlerWrapper<TRequest, TResponse>
	where TRequest : struct, IStreamCommand<TResponse> {
	private readonly StreamHandlerDelegate<TRequest, TResponse> rootHandler;

	public StreamCommandStructHandlerWrapper(
		IStreamCommandHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (StreamHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class QueryClassHandlerWrapper<TRequest, TResponse>
	where TRequest : class, IQuery<TResponse> {
	private readonly CommonHandlerDelegate<TRequest, TResponse> rootHandler;

	public QueryClassHandlerWrapper(
		IQueryHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (CommonHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class QueryStructHandlerWrapper<TRequest, TResponse>
	where TRequest : struct, IQuery<TResponse> {
	private readonly CommonHandlerDelegate<TRequest, TResponse> rootHandler;

	public QueryStructHandlerWrapper(
		IQueryHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (CommonHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public ValueTask<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class StreamQueryClassHandlerWrapper<TRequest, TResponse>
	where TRequest : class, IStreamQuery<TResponse> {
	private readonly StreamHandlerDelegate<TRequest, TResponse> rootHandler;

	public StreamQueryClassHandlerWrapper(
		IStreamQueryHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (StreamHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}

public sealed class StreamQueryStructHandlerWrapper<TRequest, TResponse>
	where TRequest : struct, IStreamQuery<TResponse> {
	private readonly StreamHandlerDelegate<TRequest, TResponse> rootHandler;

	public StreamQueryStructHandlerWrapper(
		IStreamQueryHandler<TRequest, TResponse> concreteHandler,
		IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> pipelineBehaviours
	) {
		var handler = (StreamHandlerDelegate<TRequest, TResponse>)concreteHandler.Handle;

		foreach (var pipeline in pipelineBehaviours.Reverse()) {
			var handlerCopy = handler;
			var pipelineCopy = pipeline;
			handler = (message, ct) => pipelineCopy.Handle(message, handlerCopy, ct);
		}

		rootHandler = handler;
	}

	public IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct) => rootHandler(request, ct);
}
