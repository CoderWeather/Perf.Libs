namespace Mediator.Pipelines;

public interface IPipelineBehavior<TMessage, TResponse>
	where TMessage : IMessage {
	ValueTask<TResponse> Handle(
		TMessage message,
		CommonHandlerDelegate<TMessage, TResponse> next,
		CancellationToken ct
	);
}

public interface IStreamPipelineBehavior<TMessage, TResponse>
	where TMessage : IStreamMessage {
	IAsyncEnumerable<TResponse> Handle(
		TMessage message,
		StreamHandlerDelegate<TMessage, TResponse> next,
		CancellationToken ct
	);
}
