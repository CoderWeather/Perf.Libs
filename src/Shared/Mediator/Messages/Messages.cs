namespace Mediator.Messages;

public interface IMessage { }

public interface IStreamMessage { }

public interface IRequest : IMessage { }

public interface ICommand : IMessage { }

public interface IQuery : IMessage { }

public interface IRequest<out TResponse> : IRequest { }

public interface ICommand<out TResponse> : ICommand { }

public interface IQuery<out TResponse> : IQuery { }

public interface INotification : IMessage { }

public interface IStreamRequest<out TResponse> : IStreamMessage { }

public interface IStreamCommand<out TResponse> : IStreamMessage { }

public interface IStreamQuery<out TResponse> : IStreamMessage { }
