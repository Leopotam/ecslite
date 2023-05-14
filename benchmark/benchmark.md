With systems cast to interfaces:

|                      Method |     Mean |     Error |    StdDev |   Median |   Gen 0 | Allocated |
|---------------------------- |---------:|----------:|----------:|---------:|--------:|----------:|
| With debug checks           | 10.96 ms |  0.049 ms |  0.041 ms |          | 31.2500 |    163 KB |
| Filters enabled             | 9.690 ms | 0.1115 ms | 0.0988 ms |          | 31.2500 |    156 KB |
| Default version             | 8.642 ms | 0.0926 ms | 0.0821 ms |          | 31.2500 |    156 KB |

System cast to interface removed:

|                      Method |     Mean |     Error |    StdDev |   Median |   Gen 0 | Allocated |
| With debug checks           | 50.14 us |  0.425 us |  0.397 us |  24.2920 |  0.1221 |    100 KB |
| Filters enabled             | 40.58 us |  0.776 us |  0.981 us |  22.8271 |  0.0610 |     93 KB |
| Default version             | 41.42 us |  0.237 us |  0.222 us |  22.8271 |  0.0610 |     93 KB |
