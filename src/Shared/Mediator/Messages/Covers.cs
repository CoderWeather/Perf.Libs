namespace Mediator.Messages;

public interface ICoverRequest<out TRequest> where TRequest : IRequest {
	TRequest Map();
}

public interface ICoverCommand<out TCommand> where TCommand : ICommand {
	TCommand Map();
}

public interface ICoverQuery<out TQuery> where TQuery : IQuery {
	TQuery Map();
}

public interface ICoveredBy<TCover> { }
