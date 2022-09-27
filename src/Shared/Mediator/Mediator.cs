namespace Mediator;

public interface IMediator : ISender, IPublisher { }

public interface IPublisher {
	ValueTask Publish(INotification notification, CancellationToken ct = default);

	// ValueTask Publish(object notification, CancellationToken ct = default);
}

public interface ISender {
	// ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);

	// ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);

	// ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);

	// ValueTask<object?> Send(object message, CancellationToken ct = default);

	// IAsyncEnumerable<TResponse> CreateStream<TResponse>(
	// 	IStreamQuery<TResponse> query,
	// 	CancellationToken ct = default
	// );
	//
	// IAsyncEnumerable<TResponse> CreateStream<TResponse>(
	// 	IStreamRequest<TResponse> request,
	// 	CancellationToken ct = default
	// );
	//
	// IAsyncEnumerable<TResponse> CreateStream<TResponse>(
	// 	IStreamCommand<TResponse> command,
	// 	CancellationToken ct = default
	// );
	//
	// IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default);
}

public delegate ValueTask<TResponse> CommonHandlerDelegate<in TMessage, TResponse>(
	TMessage message,
	CancellationToken ct
) where TMessage : IMessage;

public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<in TMessage, out TResponse>(
	TMessage message,
	CancellationToken ct
) where TMessage : IStreamMessage;
