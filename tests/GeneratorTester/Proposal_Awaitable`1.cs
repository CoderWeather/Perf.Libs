// ReSharper disable UnusedTypeParameter
// ReSharper disable ParameterHidesMember
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace GeneratorTester;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.EntityFrameworkCore;
using Perf.Holders;

/*
 * Proposal AwaitableResult
 * Cannot be implemented until AsyncMethodBuilder would support two and more type arguments
 */

public enum ResultAwaitableOpenStatus {
    Value = 1,
    Exception = 2,
    Pending = 3
}

interface IAwaitableResultHolder<T> {
    public ValueTask<T> Value { get; }
    public bool HasValue { get; }
    public AggregateException Exception { get; }
    public ResultAwaitableOpenStatus Status { get; }
}

[AsyncMethodBuilder(typeof(AsyncAwaitableResultMethodBuilder<>))]
[StructLayout(LayoutKind.Auto)]
public readonly struct AwaitableResult<T> : IAwaitableResultHolder<T>
    where T : notnull {
#region Constructors
    enum ResultAwaitableState {
        Uninitialized = 0,
        Value = 1,
        Exception = 2,
        PendingSelf = 3
    }

    public AwaitableResult() {
        state = ResultAwaitableState.Uninitialized;
        value = default!;
        exc = null;
        task = null;
    }

    public AwaitableResult(T value) {
        state = ResultAwaitableState.Value;
        this.value = value;
        task = null;
        exc = null;
    }

    public AwaitableResult(Task<T> valueTask) {
        state = ResultAwaitableState.Value;
        value = default!;
        task = valueTask ?? throw new ArgumentNullException(nameof(valueTask));
        exc = null;
    }

    public AwaitableResult(AggregateException exception) {
        state = ResultAwaitableState.Exception;
        value = default!;
        exc = exception ?? throw new ArgumentNullException(nameof(exception));
        task = null;
    }

    public AwaitableResult(Exception exception) {
        state = ResultAwaitableState.Exception;
        value = default!;
        exc = new(exception ?? throw new ArgumentNullException(nameof(exception)));
        task = null;
    }

    public AwaitableResult(Task<AwaitableResult<T>> taskSelf) {
        state = ResultAwaitableState.PendingSelf;
        value = default!;
        exc = null!;
        task = taskSelf;
    }

    public AwaitableResult(Task<Result<T, Exception>> taskSelf) {
        state = ResultAwaitableState.PendingSelf;
        value = default!;
        exc = null!;
        task = taskSelf;
    }
#endregion

    readonly T value;
    readonly AggregateException? exc;
    readonly ResultAwaitableState state;
    readonly Task? task;

#region Public Properties
    public ValueTask<T> Value =>
        state switch {
            // ResultAwaitableState.Value         => task is { } t ? new ValueTask<T>(t) : new(value),
            ResultAwaitableState.Value         => new(value),
            ResultAwaitableState.Exception     => throw new InvalidOperationException("Invalid access to Exception"),
            ResultAwaitableState.PendingSelf   => throw new InvalidOperationException("Invalid access to Self"),
            ResultAwaitableState.Uninitialized => throw new InvalidOperationException("Invalid access"),
            _                                  => throw new InvalidOperationException()
        };

    public AggregateException Exception =>
        state switch {
            ResultAwaitableState.Exception     => exc!,
            ResultAwaitableState.Value         => throw new InvalidOperationException("Invalid access to Value"),
            ResultAwaitableState.PendingSelf   => throw new InvalidOperationException("Invalid access to Self"),
            ResultAwaitableState.Uninitialized => throw new InvalidOperationException("Invalid access"),
            _                                  => throw new InvalidOperationException()
        };

    public bool HasValue =>
        state switch {
            ResultAwaitableState.Value         => true,
            ResultAwaitableState.Exception     => false,
            ResultAwaitableState.PendingSelf   => false,
            ResultAwaitableState.Uninitialized => throw new InvalidOperationException("Invalid access"),
            _                                  => throw new InvalidOperationException()
        };

    public ResultAwaitableOpenStatus Status =>
        state switch {
            ResultAwaitableState.Value         => ResultAwaitableOpenStatus.Value,
            ResultAwaitableState.Exception     => ResultAwaitableOpenStatus.Exception,
            ResultAwaitableState.PendingSelf   => ResultAwaitableOpenStatus.Pending,
            ResultAwaitableState.Uninitialized => throw new InvalidOperationException("Invalid access"),
            _                                  => throw new InvalidOperationException()
        };
#endregion

#pragma warning disable CA2012
    public ValueTaskAwaiter<Result<T, Exception>> GetAwaiter() => GetTask().GetAwaiter();
    // public ValueTaskAwaiter<AwaitableResult<T>> GetAwaiter() => GetTask().GetAwaiter();
#pragma warning restore CA2012
    async ValueTask<Result<T, Exception>> GetTask() {
        // async ValueTask<AwaitableResult<T>> GetTask() {
        switch (state) {
            case ResultAwaitableState.Value:
                if (task is Task<T> taskValue) {
                    try {
                        return await taskValue.ConfigureAwait(false);
                    } catch (Exception e) {
                        return e;
                    }
                }

                return value;
            case ResultAwaitableState.Exception:
                return exc!;
            case ResultAwaitableState.PendingSelf:
                if (task is Task<Result<T, Exception>> taskResult) {
                    try {
                        return await taskResult.ConfigureAwait(false);
                    } catch (Exception e) {
                        return e;
                    }
                }

                if (task is Task<AwaitableResult<T>> taskSelf) {
                    try {
                        return await taskSelf.ConfigureAwait(false);
                    } catch (Exception e) {
                        return e;
                    }
                }

                throw new InvalidOperationException($"Invalid access to pending AwaitableResult<{typeof(T).Name}>");
            case ResultAwaitableState.Uninitialized:
                throw new InvalidOperationException("Invalid access");
            default:
                throw new InvalidOperationException();
        }
    }

    public static implicit operator AwaitableResult<T>(T value) => new(value);
    public static implicit operator AwaitableResult<T>(Task<T> value) => new(value);
    public static implicit operator AwaitableResult<T>(AggregateException exc) => new(exc);
    public static implicit operator AwaitableResult<T>(Exception exc) => new(exc);
    public static implicit operator AwaitableResult<T>(Task<AwaitableResult<T>> taskSelf) => new(taskSelf);
    public static implicit operator AwaitableResult<T>(Result<T, Exception> result) => result.IsOk ? new(result.Ok) : new(result.Error);

    public static implicit operator Result<T, Exception>(AwaitableResult<T> result) => result.HasValue ? new(result.value) : new((Exception)result.exc!);
}

static class AwaitableResultUsage {
    public static async AwaitableResult<int> Foo() {
        var q = Array.Empty<int>().AsQueryable();
        var defaultDbResult = await q.ExecuteDeleteAsync();
        return defaultDbResult;
    }
}

[StructLayout(LayoutKind.Auto)]
public struct AsyncAwaitableResultMethodBuilder<T>
    where T : notnull {
    AsyncValueTaskMethodBuilder<Result<T, Exception>> methodBuilder;
    Result<T, Exception> result;
    bool haveResult;
    bool useBuilder;

    public static AsyncAwaitableResultMethodBuilder<T> Create() {
        return new() {
            methodBuilder = AsyncValueTaskMethodBuilder<Result<T, Exception>>.Create()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine {
        methodBuilder.Start(ref stateMachine);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) {
        methodBuilder.SetStateMachine(stateMachine);
    }

    public void SetResult(T result) {
        if (useBuilder) {
            methodBuilder.SetResult(result);
        } else {
            haveResult = true;
            this.result = result;
        }
    }

    public void SetException(Exception exception) {
        if (useBuilder) {
            methodBuilder.SetException(exception);
        } else {
            haveResult = true;
            result = exception;
        }
    }

    public AwaitableResult<T> Task {
        get {
            if (haveResult) {
                return result;
            }

            useBuilder = true;
            return new(methodBuilder.Task.AsTask());
        }
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine
    )
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        useBuilder = true;
        methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
    }

    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine
    )
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine {
        useBuilder = true;
        methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}
