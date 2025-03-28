// namespace GeneratorTester;
//
// using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;
//
// public enum TaskResultOpenState {
//     Value = 1,
//     Exception = 2,
//     Pending = 3
// }
//
// interface ITaskResultHolder<TOk, TError>
//     where TOk : notnull
//     where TError : notnull {
//     public ValueTask<TOk> Ok { get; }
//     public ValueTask<TError> Error { get; }
//
//     public bool IsOk { get; }
//
//     // public AggregateException Exception { get; }
//     public TaskResultOpenState State { get; }
// }
//
// [StructLayout(LayoutKind.Auto)]
// readonly struct TaskResultHolder<T> : ITaskResultHolder<T, string>
//     where T : notnull {
//     enum TaskResultState {
//         Uninitialized = 0,
//         Ok = 1,
//         Error = 2,
//         Exception = 3,
//         PendingSelf = 4
//     }
//
//     public TaskResultHolder() {
//         ok = default!;
//         error = null!;
//         state = TaskResultState.Uninitialized;
//         task = null;
//     }
//
//     readonly T ok;
//     readonly string error;
//     readonly TaskResultState state;
//     readonly Task? task;
//
//     public ValueTask<T> Ok => ValueTask.FromResult(ok);
//     public ValueTask<string> Error => ValueTask.FromResult(error);
//     public bool IsOk => true;
//     public TaskResultOpenState State => (TaskResultOpenState)state;
//
//     public ValueTaskAwaiter<TaskResultHolder<T>> GetAwaiter() => GetTask().GetAwaiter();
//
//     async ValueTask<TaskResultHolder<T>> GetTask() {
//         switch (state) {
//             case TaskResultState.Ok:
//                 return default;
//                 // if (task is Task<T> okTask) {
//                 //     try {
//                 //         return new(await okTask.ConfigureAwait(false));
//                 //     } catch (Exception e) {
//                 //         return new(e);
//                 //     }
//                 // }
//
//                 break;
//             case TaskResultState.Exception:
//                 return default;
//                 // if (task is Task<Exception> errorTask) {
//                 //     try {
//                 //         return new(await errorTask.ConfigureAwait(false));
//                 //     } catch (Exception e) {
//                 //         return new(e);
//                 //     }
//                 // }
//
//                 break;
//         }
//     }
// }
//
// public struct AsyncTaskResultHolderMethodBuilder<T>
//     where T : notnull {
//     AsyncValueTaskMethodBuilder<TaskResultHolder<T>> methodBuilder;
//     TaskResultHolder<T> result;
//     bool haveResult;
//     bool useBuilder;
//
//     public static AsyncTaskResultHolderMethodBuilder<T> Create() {
//         return new() {
//             methodBuilder = AsyncValueTaskMethodBuilder<TaskResultHolder<T>>.Create()
//         };
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
//         methodBuilder.Start(ref stateMachine);
//     }
//
//     public void SetStateMachine(T result) {
//
//     }
// }
