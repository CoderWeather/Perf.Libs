namespace Mediator.Handlers;

#region Markers

public interface IMirrorRequestHandler<TMirrorRequest> where TMirrorRequest : IMirrorMessage { }

public interface IMirrorCommandHandler<TMirrorCommand> where TMirrorCommand : IMirrorMessage { }

public interface IMirrorQueryHandler<TMirrorQuery> where TMirrorQuery : IMirrorMessage { }

#endregion

#region Use By Generator

public interface IMirrorRequestHandler<out TOriginRequest, TOriginResponse, in TMirrorRequest, TMirrorResponse>
	where TOriginRequest : IRequest<TOriginResponse>
	where TMirrorRequest : IMirrorRequest<TOriginRequest, TOriginResponse, TMirrorResponse> {
	ValueTask<TMirrorResponse> Handle(TMirrorRequest request,
		CommonHandlerDelegate<TOriginRequest, TOriginResponse> originHandlerDelegate,
		CancellationToken ct);
}

public interface IMirrorCommandHandler<out TOriginCommand, TOriginResponse, in TMirrorCommand, TMirrorResponse>
	where TOriginCommand : ICommand<TOriginResponse>
	where TMirrorCommand : IMirrorCommand<TOriginCommand, TOriginResponse, TMirrorResponse> {
	ValueTask<TMirrorResponse> Handle(TMirrorCommand command,
		CommonHandlerDelegate<TOriginCommand, TOriginResponse> originHandlerDelegate,
		CancellationToken ct);
}

public interface IMirrorQueryHandler<out TOriginQuery, TOriginResponse, in TMirrorQuery, TMirrorResponse>
	where TOriginQuery : IQuery<TOriginResponse>
	where TMirrorQuery : IMirrorQuery<TOriginQuery, TOriginResponse, TMirrorResponse> {
	ValueTask<TMirrorResponse> Handle(TMirrorQuery query,
		CommonHandlerDelegate<TOriginQuery, TOriginResponse> originHandlerDelegate,
		CancellationToken ct);
}

#endregion
