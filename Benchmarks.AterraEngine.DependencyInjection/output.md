| Method                                                      | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| Microsoft_AddBuildAndRetrieve_SingleDependency_Transient    |  47.93 us | 0.248 us | 0.220 us |  1.00 |    0.01 | 2.4414 | 1.0986 |  41.61 KB |        1.00 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Transient |  43.24 us | 0.523 us | 0.489 us |  0.90 |    0.01 | 2.5635 | 0.6104 |  42.31 KB |        1.02 |
| Microsoft_AddBuildAndRetrieve_SingleDependency_Singleton    |  14.49 us | 0.070 us | 0.066 us |  0.30 |    0.00 | 0.9918 | 0.2441 |  16.36 KB |        0.39 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Singleton |  33.86 us | 0.194 us | 0.181 us |  0.71 |    0.00 | 1.0986 | 0.3052 |  18.89 KB |        0.45 |
| Microsoft_AddBuildAndRetrieve_SingleDependency_Scoped       | 204.20 us | 4.006 us | 5.615 us |  4.26 |    0.12 | 0.9766 | 0.4883 |  20.45 KB |        0.49 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Scoped    |  30.98 us | 0.129 us | 0.121 us |  0.65 |    0.00 | 1.0986 | 0.2441 |   18.9 KB |        0.45 |
