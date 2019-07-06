using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FunWithAwaitables
{
    public static class Program
    {
        static void Main() => BenchmarkRunner.Run<Awaitable>();

        static async Task Main2()
        {
            await new Awaitable().ViaTaskLike();
            Console.WriteLine(Volatile.Read(ref AllocCounters.StateBox)); // 2
            Console.WriteLine(Volatile.Read(ref AllocCounters.TaskSource)); // 2
            Console.WriteLine(Volatile.Read(ref AllocCounters.SetStateMachine)); // 0
        }
    }

    [MemoryDiagnoser, ShortRunJob]
    public class Awaitable
    {
        const int OperationsPerInvoke = 1;
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Description = nameof(Task<int>), Baseline = true)]
        public async Task<int> ViaTask()
        {
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
                sum += await Inner(1, 2).ConfigureAwait(false);
            return sum;
            static async Task<int> Inner(int x, int y)
            {
                int i = x;
                await Task.Yield();
                i *= y;
                await Task.Yield();
                return 5 * i;
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Description = nameof(ValueTask<int>))]
        public async ValueTask<int> ViaValueTask()
        {
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
                sum += await Inner(1, 2).ConfigureAwait(false);
            return sum;
            static async ValueTask<int> Inner(int x, int y)
            {
                int i = x;
                await Task.Yield();
                i *= y;
                await Task.Yield();
                return 5 * i;
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Description = nameof(TaskLike<int>))]
        public async TaskLike<int> ViaTaskLike()
        {
            int sum = 0;
            for (int i = 0; i < OperationsPerInvoke; i++)
                sum += await Inner(1, 2).ConfigureAwait(false);
            return sum;
            static async TaskLike<int> Inner(int x, int y)
            {
                int i = x;
                await Task.Yield();
                i *= y;
                await Task.Yield();
                return 5 * i;
            }
        }
    }
}


