| Method                                                      |      Mean |    Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------------------------------------------|----------:|---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Microsoft_AddBuildAndRetrieve_SingleDependency_Transient    |  48.55 us | 0.207 us |  0.184 us |  48.52 us |  1.00 |    0.01 | 2.4414 | 1.0986 |  41.61 KB |        1.00 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Transient |  69.08 us | 0.567 us |  0.531 us |  69.04 us |  1.42 |    0.01 | 2.8076 | 2.6855 |   46.6 KB |        1.12 |
| Microsoft_AddBuildAndRetrieve_SingleDependency_Singleton    |  15.14 us | 0.226 us |  0.211 us |  15.11 us |  0.31 |    0.00 | 0.9918 | 0.2441 |  16.36 KB |        0.39 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Singleton |  65.88 us | 0.446 us |  0.417 us |  65.76 us |  1.36 |    0.01 | 1.3428 | 1.2207 |  23.19 KB |        0.56 |
| Microsoft_AddBuildAndRetrieve_SingleDependency_Scoped       | 219.98 us | 4.348 us | 11.455 us | 223.85 us |  4.53 |    0.24 | 0.9766 | 0.4883 |  20.45 KB |        0.49 |
| AterraEngine_AddBuildAndRetrieve_SingleDependency_Scoped    |  66.44 us | 0.979 us |  0.916 us |  66.00 us |  1.37 |    0.02 | 1.3428 | 1.2207 |  23.19 KB |        0.56 |
