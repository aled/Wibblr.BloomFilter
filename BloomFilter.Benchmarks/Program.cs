using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace BloomFilter.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 0, iterationCount: 1)]
    public class BloomFilterAdd
    {
        private const int N = 10000;

        // Items to add to filter
        private readonly byte[] data = new byte[10];

        private List<byte[]> items = Enumerable.Range(0, 15000)
               .Select(x => Encoding.UTF8.GetBytes(x.ToString()))
               .ToList();

        private readonly BloomFilter a = new BloomFilter(N, 0.01);
        private readonly IBloomFilter b = FilterBuilder.Build(N, 0.01, HashMethod.XXHash64);

        [Benchmark]
        public int A()
        {
            int x = 0;

            for (int i = 0; i < 10000; i++)
            {
                a.Add(items[i]);
            }
            for (int i = 5000; i < 15000; i++)
            {
                if (a.MayContain(items[i]))
                {
                    x++;
                }
            }
            return x;
        }


        [Benchmark]
        public int B()
        {
            int x = 0;

            for (int i = 0; i < 10000; i++)
            {
                b.Add(items[i]);
            }
            for (int i = 5000; i < 15000; i++)
            {
                if (b.Contains(items[i]))
                {
                    x++;
                }
            }
            return x;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BloomFilterAdd>();
        }
    }
}
