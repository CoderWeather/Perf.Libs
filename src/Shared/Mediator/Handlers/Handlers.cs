namespace Mediator.Handlers;

#region Source Generator Markers

public interface IRequestHandler<in TRequest> where TRequest : IRequest { }

public interface ICommandHandler<in TCommand> where TCommand : ICommand { }

public interface IQueryHandler<in TQuery> where TQuery : IQuery { }

#endregion

public interface IRequestHandler<in TRequest, TResponse>
	where TRequest : IRequest<TResponse> {
	ValueTask<TResponse> Handle(TRequest request, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResponse>
	where TCommand : ICommand<TResponse> {
	ValueTask<TResponse> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResponse>
	where TQuery : IQuery<TResponse> {
	ValueTask<TResponse> Handle(TQuery query, CancellationToken ct);
}

public interface INotificationHandler<in TNotification>
	where TNotification : class, INotification {
	ValueTask Handle(TNotification notification, CancellationToken ct);
}

public interface IStreamCommandHandler<in TCommand, out TResponse>
	where TCommand : IStreamCommand<TResponse> {
	IAsyncEnumerable<TResponse> Handle(TCommand command, CancellationToken ct);
}

public interface IStreamQueryHandler<in TQuery, out TResponse>
	where TQuery : IStreamQuery<TResponse> {
	IAsyncEnumerable<TResponse> Handle(TQuery query, CancellationToken ct);
}

public interface IStreamRequestHandler<in TRequest, out TResponse>
	where TRequest : IStreamRequest<TResponse> {
	IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct);
}
