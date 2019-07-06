Exploration of custom awaitables; typical results:

|    Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------- |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|      Task | 1.449 us | 0.0229 us | 0.0013 us |  1.00 | 0.0381 |     - |     - |     248 B |
| ValueTask | 1.418 us | 0.0937 us | 0.0051 us |  0.98 | 0.0420 |     - |     - |     264 B |
|  TaskLike | 1.324 us | 0.2906 us | 0.0159 us |  0.91 | 0.0191 |     - |     - |      96 B |

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e.

``` c#
public async Task<int> ViaTask() {...}
public async ValueTask<int> ViaValueTask() {...}
public async TaskLike<int> ViaTaskLike() {...}
```

All of them have the same threading/execution-context/sync-context semantics; there's no cheating.