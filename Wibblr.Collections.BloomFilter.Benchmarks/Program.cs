using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using NetCoreBf = BloomFilter;

namespace Wibblr.Collections.BloomFilter.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class BloomFilterAdd
    {
        private const int N = 10000;

        private readonly List<byte[]> items = Enumerable.Range(0, N * 2)
               .Select(x => Encoding.UTF8.GetBytes(x.ToString()))
               .ToList();

        private readonly BloomFilter a = new(N, 0.01);
        private readonly NetCoreBf.IBloomFilter b = NetCoreBf.FilterBuilder.Build(N, 0.01, NetCoreBf.HashMethod.XXHash64);

        [Benchmark]
        public int BloomFilter_Wibblr()
        {
            int x = 0;

            for (int i = 0; i < 10000; i++)
            {
                a.Add(items[i]);
            }
            for (int i = 5000; i < 15000; i++)
            {
                if (a.Contains(items[i]))
                {
                    x++;
                }
            }
            return x;
        }


        [Benchmark]
        public int BloomFilter_NetCore()
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
        public static void Main()
        {
            BenchmarkRunner.Run<BloomFilterAdd>();
        }
    }
}
