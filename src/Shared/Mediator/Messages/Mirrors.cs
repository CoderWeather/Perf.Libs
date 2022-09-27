namespace Mediator.Messages;

public interface IMirrorMessage : IMessage { }

#region Markers

public interface IMirrorRequest<TOriginRequest> : IMirrorMessage where TOriginRequest : IRequest { }

public interface IMirrorRequest<TOriginRequest, TResponse> where TOriginRequest : IRequest { }

public interface IMirrorCommand<TOriginCommand> : IMirrorMessage where TOriginCommand : ICommand { }

public interface IMirrorCommand<TOriginCommand, TResponse> : IMirrorMessage where TOriginCommand : ICommand { }

public interface IMirrorQuery<TOriginQuery> : IMirrorMessage where TOriginQuery : IQuery { }

public interface IMirrorQuery<TOriginQuery, TResponse> : IMirrorMessage where TOriginQuery : IQuery { }

#endregion

#region Use by generator

public interface IMirrorRequest<TOriginRequest, TOriginResponse, TResponse> : IMirrorMessage
	where TOriginRequest : IRequest<TOriginResponse> { }

public interface IMirrorQuery<TOriginQuery, TOriginResponse, TResponse> : IMirrorMessage
	where TOriginQuery : IQuery<TOriginResponse> { }

public interface IMirrorCommand<TOriginCommand, TOriginResponse, TResponse> : IMirrorMessage
	where TOriginCommand : ICommand<TOriginResponse> { }

#endregion
