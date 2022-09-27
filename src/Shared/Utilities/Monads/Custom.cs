namespace Utilities.Monads;

public enum NonTargetError {
	NotFound = 1,
	InsertFailed,
	UpdateFailed,
	DeleteFailed
}

public readonly partial struct NonTargetErrorResult : IResultMonad<Unit, NonTargetError> { }

public readonly partial struct NonTargetErrorResult<T> : IResultMonad<T, NonTargetError> where T : notnull { }

public readonly partial struct ErrorResult<T> : IResultMonad<T, string> where T : notnull { }

public readonly partial struct ExceptionResult<T> : IResultMonad<T, Exception> where T : notnull { }
