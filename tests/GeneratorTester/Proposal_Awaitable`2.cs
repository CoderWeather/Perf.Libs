// // ReSharper disable UnusedTypeParameter
//
// namespace GeneratorTester;
//
// using System.Runtime.CompilerServices;
// using System.Security;
// using LinqToDB;
//
// /*
//  * Proposal AwaitableResult
//  * Cannot be implemented until AsyncMethodBuilder would support two and more type arguments
//  */
//
// enum ResultAwaitableState {
//     Uninitialized = 0,
//     Ok = 1,
//     Error = 2,
//     Exception = 3,
//     Faulted = 4,
//     Self = 5
// }
//
// interface IAwaitableResultHolder<TOk, TError> {
//     public ValueTask<TOk> Ok { get; }
//     public ValueTask<TError> Error { get; }
//     public bool IsOk { get; }
//     public bool IsFaulted { get; }
//     public AggregateException Exception { get; }
// }
//
// [AsyncMethodBuilder(typeof(AsyncAwaitableResultMethodBuilder<,>))]
// public readonly struct AwaitableResult<TOk, TError> : IAwaitableResultHolder<TOk, TError> {
//     public AwaitableResult() {
//         state = ResultAwaitableState.Uninitialized;
//         ok = default!;
//         error = default!;
//         task = null;
//         exc = null;
//     }
//
//     public AwaitableResult(TOk ok) {
//         state = ResultAwaitableState.Ok;
//         this.ok = ok;
//         error = default!;
//         task = null;
//         exc = null;
//     }
//
//     public AwaitableResult(TError error) {
//         state = ResultAwaitableState.Error;
//         ok = default!;
//         this.error = error;
//         task = null;
//         exc = null;
//     }
//
//     public AwaitableResult(Task<TOk> ok) {
//         state = ResultAwaitableState.Ok;
//         this.ok = default!;
//         error = default!;
//         task = ok;
//         exc = null;
//     }
//
//     public AwaitableResult(Task<TError> error) {
//         state = ResultAwaitableState.Error;
//         ok = default!;
//         this.error = default!;
//         task = error;
//         exc = null;
//     }
//
//     public AwaitableResult(AggregateException exc) {
//         state = ResultAwaitableState.Faulted;
//         ok = default!;
//         error = default!;
//         task = null;
//         this.exc = exc;
//     }
//
//     public AwaitableResult(Exception exc) {
//         state = ResultAwaitableState.Faulted;
//         ok = default!;
//         error = default!;
//         task = null;
//         this.exc = new(exc);
//     }
//
//     public AwaitableResult(Task<AwaitableResult<TOk, TError>> selfTask) {
//         state = ResultAwaitableState.Self;
//         ok = default!;
//         error = default!;
//         task = selfTask;
//         exc = null;
//     }
//
//     readonly TOk ok;
//     readonly TError error;
//     readonly ResultAwaitableState state;
//     readonly Task? task;
//     readonly AggregateException? exc;
//
//     public ValueTask<TOk> Ok =>
//         state switch {
//             ResultAwaitableState.Ok    => task is Task<TOk> t ? new ValueTask<TOk>(t) : new(ok),
//             ResultAwaitableState.Error => default,
//             _                          => throw new InvalidOperationException()
//         };
//
//     public ValueTask<TError> Error =>
//         state switch {
//             ResultAwaitableState.Error => task is Task<TError> t ? new ValueTask<TError>(t) : new(error),
//             ResultAwaitableState.Ok    => default,
//             _                          => throw new InvalidOperationException()
//         };
//
//     public bool IsOk => state is ResultAwaitableState.Ok;
//     public bool IsFaulted => state is ResultAwaitableState.Faulted;
//     public AggregateException Exception => exc!;
//
//     public TaskAwaiter<AwaitableResult<TOk, TError>> GetAwaiter() =>
//         state switch {
//             ResultAwaitableState.Self when task is Task<AwaitableResult<TOk, TError>> t => t.ContinueWith(static rt => rt switch {
//                     { IsCompleted: true } => rt.Result,
//                     { IsFaulted: true }   => rt.Exception,
//                     _                     => throw new InvalidOperationException()
//                 }
//             ).GetAwaiter(),
//             ResultAwaitableState.Ok when task is Task<TOk> t => t.ContinueWith(
//                 static okt => okt switch {
//                     { IsCompleted: true } => new AwaitableResult<TOk, TError>(okt.Result),
//                     { IsFaulted: true }   => okt.Exception!,
//                     _                     => throw new InvalidOperationException()
//                 },
//                 TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.OnlyOnFaulted
//             ).GetAwaiter(),
//             ResultAwaitableState.Ok => Task.FromResult(new AwaitableResult<TOk, TError>(ok)).GetAwaiter(),
//             ResultAwaitableState.Error when task is Task<TError> t => t.ContinueWith(
//                 static ert => ert switch {
//                     { IsCompleted: true } => new AwaitableResult<TOk, TError>(ert.Result),
//                     { IsFaulted: true }   => ert.Exception!,
//                     _                     => throw new InvalidOperationException()
//                 },
//                 TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.OnlyOnFaulted
//             ).GetAwaiter(),
//             ResultAwaitableState.Error         => Task.FromResult(new AwaitableResult<TOk, TError>(error)).GetAwaiter(),
//             ResultAwaitableState.Faulted       => Task.FromResult(new AwaitableResult<TOk, TError>(exc!)).GetAwaiter(),
//             ResultAwaitableState.Uninitialized => throw new InvalidOperationException("some things"),
//             _                                  => throw new InvalidOperationException()
//         };
//
//     public static implicit operator AwaitableResult<TOk, TError>(TOk ok) => new(ok);
//     public static implicit operator AwaitableResult<TOk, TError>(TError error) => new(error);
//     public static implicit operator AwaitableResult<TOk, TError>(Task<TOk> ok) => new(ok);
//     public static implicit operator AwaitableResult<TOk, TError>(Task<TError> error) => new(error);
//     public static implicit operator AwaitableResult<TOk, TError>(AggregateException exc) => new(exc);
//     public static implicit operator AwaitableResult<TOk, TError>(Exception exc) => new(exc);
// }
//
// static class AwaitableResultUsage {
//     public static async Task Foo() {
//         var q = Array.Empty<int>().AsQueryable();
//         try {
//             var defaultDbResult = await q.DeleteAsync();
//             _ = defaultDbResult;
//         } catch (Exception ex) {
//             AwaitableResult<int, Exception> r = new(ex);
//             var t = await r;
//             _ = t;
//         }
//     }
// }
//
// public struct AsyncAwaitableResultMethodBuilder<TOk, TError> {
//     // AsyncTaskMethodBuilder<TOk> okMethodBuilder;
//     // AsyncTaskMethodBuilder<TError> errorMethodBuilder;
//     // TOk ok;
//     // TError error;
//     // bool haveOk;
//     // bool haveError;
//     AsyncTaskMethodBuilder<AwaitableResult<TOk, TError>> methodBuilder;
//     AwaitableResult<TOk, TError> result;
//     bool haveResult;
//     bool useBuilder;
//
//     public static AsyncAwaitableResultMethodBuilder<TOk, TError> Create() {
//         return new() {
//             methodBuilder = AsyncTaskMethodBuilder<AwaitableResult<TOk, TError>>.Create()
//             // okMethodBuilder = AsyncTaskMethodBuilder<TOk>.Create(),
//             // errorMethodBuilder = AsyncTaskMethodBuilder<TError>.Create()
//         };
//     }
//
//     public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
//         methodBuilder.Start(ref stateMachine);
//     }
//
//     public void SetStateMachine(IAsyncStateMachine stateMachine) {
//         methodBuilder.SetStateMachine(stateMachine);
//     }
//
//     public void SetResult(AwaitableResult<TOk, TError> awaitableResult) {
//         if (useBuilder) {
//             methodBuilder.SetResult(result);
//         } else {
//             result = awaitableResult;
//             haveResult = true;
//         }
//     }
//
//     public void SetException(Exception exception) => methodBuilder.SetException(exception);
//
//     public AwaitableResult<TOk, TError> Task {
//         get {
//             if (haveResult) {
//                 return result;
//                 // return new AwaitableResult<TOk, TError>(result);
//             }
//
//             useBuilder = true;
//             return new(methodBuilder.Task);
//         }
//     }
//
//     public void AwaitOnCompleted<TAwaiter, TStateMachine>(
//         ref TAwaiter awaiter,
//         ref TStateMachine stateMachine
//     )
//         where TAwaiter : INotifyCompletion
//         where TStateMachine : IAsyncStateMachine {
//         useBuilder = true;
//         methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
//     }
//
//     [SecuritySafeCritical]
//     public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
//         ref TAwaiter awaiter,
//         ref TStateMachine stateMachine
//     )
//         where TAwaiter : ICriticalNotifyCompletion
//         where TStateMachine : IAsyncStateMachine {
//         useBuilder = true;
//         methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
//     }
// }
