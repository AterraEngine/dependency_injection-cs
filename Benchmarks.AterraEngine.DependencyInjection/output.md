| Method                                                      | Mean     | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------------ |---------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| Microsoft_AddBuildAndRetrieve_SingleDependency_Transient    | 50.21 us | 0.566 us | 0.529 us |  1.00 | 2.4414 | 1.0986 |  41.61 KB |        1.00 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Transient | 41.94 us | 0.602 us | 0.563 us |  0.84 | 2.3804 | 1.1597 |  39.05 KB |        0.94 |
| Microsoft_AddBuildAndRetrieve_SingleDependency_Singleton    | 14.70 us | 0.287 us | 0.295 us |  0.29 | 0.9918 | 0.2441 |  16.36 KB |        0.39 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Singleton | 39.21 us | 0.353 us | 0.330 us |  0.78 | 0.9155 | 0.8545 |  15.69 KB |        0.38 |
