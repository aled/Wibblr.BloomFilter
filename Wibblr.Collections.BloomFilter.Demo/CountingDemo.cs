namespace Wibblr.Collections.BloomFilter.Demo
{
    internal class CountingDemo
    {
        private readonly int itemCount = 200000;
        private readonly double falsePositiveRatio = 0.1d;

        private CountingBloomFilter<int> _filter;

        public void RunCountingBloomFilter()
        {
            _filter = new CountingBloomFilter<int>(itemCount, falsePositiveRatio);

            Console.WriteLine($"Creating bloom filter with capacity of {_filter.RequestedCapacity} items, and false positive ratio {_filter.RequestedFalsePositiveRatio}.");
            Console.WriteLine($"  Allocating filter size={_filter.FilterSize} items ({_filter.FilterSize * sizeof(ushort) / 1024} KB); hash count={_filter.HashCount}; hash length={_filter.HashLength} bits");
            Console.WriteLine($"  Expected capacity={_filter.CapacityAtRequestedFalsePositiveRatio} at false positive ratio={double.Round(_filter.RequestedFalsePositiveRatio, 6)}");
            Console.WriteLine($"  Expected false positive ratio={double.Round(_filter.FalsePositiveRatioAtRequestedCapacity, 6)} at capacity={_filter.RequestedCapacity}");

            var t1 = DateTime.UtcNow;
            Add();
            var t2 = DateTime.UtcNow;
            Query();
            var t3 = DateTime.UtcNow;

            Console.WriteLine($"{double.Round((t2 - t1).TotalNanoseconds / itemCount, 1)}ns per increment");
            Console.WriteLine($"{double.Round((t3 - t2).TotalNanoseconds / itemCount, 1)}ns per query");
        }

        private void Add()
        {
            int i = 0;

            while (i < itemCount)
            {
                _filter.Increment(i, 1 + i / 100);
                i += 2;
            }
        }

        private void Query()
        {
            int correctCount = 0, wrongCount = 0;

            int i = 0;

            while (i < itemCount)
            {
                var expectedCount = (i % 2 == 0) ? 1 + i / 100 : 0;
                var actualCount = _filter.Count(i);
                if (actualCount == expectedCount)
                {
                    correctCount++;
                }
                else if (actualCount > expectedCount)
                {
                    wrongCount++;
                    Console.WriteLine($"{i} incorrect (expected count {expectedCount}, actual {_filter.Count(i)})");
                }
                else
                {
                    wrongCount++;
                    Console.WriteLine("Error, actual count should never be less than expected");
                }
                i++;
            }

            Console.WriteLine($"Total items queried {itemCount}, Correct count: {correctCount}, Wrong count {wrongCount} wrong counts ({double.Round(wrongCount / (double)itemCount, 6)})");
        }
    }
}