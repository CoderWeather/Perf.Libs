namespace Mediator;

public sealed class MissingMessageHandlerException : Exception {
	public object? MediatorMessage { get; }

	public MissingMessageHandlerException(object? message)
		: base("No handler registered for message type: " + message?.GetType().FullName) {
		MediatorMessage = message;
	}
}
