Exploration of custom awaitables; typical results:

for 1 inner-op:

|    Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------- |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|      Task | 1.432 us | 0.0633 us | 0.0035 us |  1.00 | 0.0381 |     - |     - |     248 B |
| ValueTask | 1.434 us | 0.0573 us | 0.0031 us |  1.00 | 0.0420 |     - |     - |     264 B |
|  TaskLike | 1.372 us | 0.0640 us | 0.0035 us |  0.96 | 0.0153 |     - |     - |      64 B |

or for 100 inner-ops:

|    Method |        Mean |     Error |   StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------- |------------:|----------:|---------:|------:|-------:|-------:|------:|----------:|
|      Task | 1,423.55 ns |  55.01 ns | 3.015 ns |  1.00 | 0.0195 |      - |     - |       3 B |
| ValueTask | 1,420.33 ns |  53.19 ns | 2.915 ns |  1.00 | 0.0195 |      - |     - |       3 B |
|  TaskLike |    67.09 ns | 105.77 ns | 5.798 ns |  0.05 | 0.0011 | 0.0004 |     - |       1 B |

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e.

``` c#
public async Task<int> ViaTask() {...}
public async ValueTask<int> ViaValueTask() {...}
public async TaskLike<int> ViaTaskLike() {...}
```

All of them have the same threading/execution-context/sync-context semantics; there's no cheating.