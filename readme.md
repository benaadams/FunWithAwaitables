Exploration of custom awaitables; typical results:

for 1 inner-op (best to show memory delta):

|    Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------- |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|      Task | 1.419 us | 0.0345 us | 0.0019 us |  1.00 | 0.0381 |     - |     - |     248 B |
| ValueTask | 1.427 us | 0.0825 us | 0.0045 us |  1.01 | 0.0420 |     - |     - |     264 B |
|  TaskLike | 1.317 us | 0.0380 us | 0.0021 us |  0.93 | 0.0095 |     - |     - |      64 B |

or for 100 inner-ops (best to show performance delta):

|    Method |        Mean |      Error |    StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------- |------------:|-----------:|----------:|------:|-------:|-------:|------:|----------:|
|      Task | 1,431.03 ns | 126.987 ns | 6.9606 ns |  1.00 | 0.0195 |      - |     - |       3 B |
| ValueTask | 1,424.12 ns |   9.393 ns | 0.5149 ns |  1.00 | 0.0195 |      - |     - |       3 B |
|  TaskLike |    66.64 ns |  55.237 ns | 3.0277 ns |  0.05 | 0.0006 | 0.0002 |     - |       1 B |

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e.

``` c#
public async Task<int> ViaTask() {...}
public async ValueTask<int> ViaValueTask() {...}
public async TaskLike<int> ViaTaskLike() {...}
```

All of them have the same threading/execution-context/sync-context semantics; there's no cheating.