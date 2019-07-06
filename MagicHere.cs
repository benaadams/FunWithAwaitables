using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace FunWithAwaitables
{
    internal static class Pool<T> where T : class
    {
        [ThreadStatic]
        private static T ts_local;

        private static readonly T[] s_global = new T[8];

        public static T TryGet()
        {
            var tmp = ts_local;
            ts_local = null;
            return tmp ?? FromPool();

            static T FromPool()
            {
                var pool = s_global;
                for (int i = 0; i < pool.Length; i++)
                {
                    var tmp = Interlocked.Exchange(ref pool[i], null);
                    if (tmp != null) return tmp;
                }
                return null;
            }
        }
        public static void TryPut(T value)
        {
            if (value != null)
            {
                if (ts_local != null)
                {
                    ts_local = value;
                    return;
                }
                ToPool(value);
            }
            static void ToPool(T value)
            {
                var pool = s_global;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (Interlocked.CompareExchange(ref pool[i], value, null) == null)
                        return;
                }
            }
        }
    }

    static class AllocCounters
    {
        public static int TaskSource, StateBox, SetStateMachine;
    }
    [AsyncMethodBuilder(typeof(TaskSource<>))]
    public readonly struct TaskLike<T>
    {

        private readonly TaskSource<T> source;
        private readonly short token;

        public TaskLike(TaskSource<T> source, short token) : this()
        {
            this.source = source;
            this.token = token;
        }

        public ValueTask<T> Task => new ValueTask<T>(source, token);

        public ValueTaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();

        public ConfiguredValueTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
            => new ValueTask<T>(source, token).ConfigureAwait(continueOnCapturedContext);
    }
    public sealed class TaskSource<T> : IValueTaskSource<T>
    {
        public static TaskSource<T> Create() => Pool<TaskSource<T>>.TryGet() ?? new TaskSource<T>();

        private TaskSource()
        {
            Interlocked.Increment(ref AllocCounters.TaskSource);
        }

        public TaskLike<T> Task => new TaskLike<T>(this, source.Version);
        public ValueTask<T> ValueTask => new ValueTask<T>(this, source.Version);

        private ManualResetValueTaskSourceCore<T> source; // needs to be mutable

        T IValueTaskSource<T>.GetResult(short token)
        {
            // we only support getting the result once; doing this recycles
            // the source and advances the token
            try
            {
                return source.GetResult(token);
            }
            finally
            {
                source.Reset();
                Pool<TaskSource<T>>.TryPut(this);
            }
        }

        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => source.GetStatus(token);

        void IValueTaskSource<T>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
            => source.OnCompleted(continuation, state, token, flags);



        public void SetException(Exception error) => source.SetException(error);
        public void SetResult(T result) => source.SetResult(result);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => StateBox<TStateMachine>.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => StateBox<TStateMachine>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
            => stateMachine.MoveNext();

        public void SetStateMachine(IAsyncStateMachine _)
        {
            Interlocked.Increment(ref AllocCounters.SetStateMachine);
        }

    }
    internal sealed class StateBox<TStateMachine>
        where TStateMachine : IAsyncStateMachine
    {
        private StateBox()
        {
            onCompleted = OnCompleted;
            Interlocked.Increment(ref AllocCounters.StateBox);
        }

        private static StateBox<TStateMachine> Create(TStateMachine stateMachine)
        {
            var box = Pool<StateBox<TStateMachine>>.TryGet() ?? new StateBox<TStateMachine>();
            box.stateMachine = stateMachine;
            return box;
        }
        private TStateMachine stateMachine;
        private readonly Action onCompleted;
        public static void AwaitOnCompleted<TAwaiter>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            => awaiter.OnCompleted(Create(stateMachine).onCompleted);

        public static void AwaitUnsafeOnCompleted<TAwaiter>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            => awaiter.UnsafeOnCompleted(Create(stateMachine).onCompleted);

        private void OnCompleted()
        {
            // extract the state
            var tmp = stateMachine;

            // recycle the instance
            stateMachine = default;
            Pool<StateBox<TStateMachine>>.TryPut(this);

            // progress the state machine
            tmp.MoveNext();
        }
    }
}
