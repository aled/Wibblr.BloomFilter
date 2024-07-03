namespace Wibblr.Collections.BloomFilter.Demo
{
    internal class Program
    {
        readonly int count = 200000;
        readonly double falsePositiveRatio = 0.1d;

        readonly BloomFilter<long> _filter;

        private Program()
        {
            _filter = new BloomFilter<long>(count, falsePositiveRatio);
        }

        static void Main()
        {
            new Program().Run();
        }

        // Use this for memory profiling
        //static void Main2()
        //{
        //    var x = new BloomFilter(10000, 0.1d);
        //
        //    while (true)
        //    {
        //        x.Add(100L);
        //        Thread.Sleep(1000);
        //    }
        //}

        public void Run()
        {
            var t1 = DateTime.UtcNow;
            Add();
            var t2 = DateTime.UtcNow;
            Query();
            var t3 = DateTime.UtcNow;

            Console.WriteLine($"{double.Round((t2 - t1).TotalNanoseconds / count, 1)}ns per add");
            Console.WriteLine($"{double.Round((t3 - t2).TotalNanoseconds / count, 1)}ns per query");
        }

        private void Add()
        {
            long i = 0;

            while (i < count)
            {
                _filter.Add(i);
                i++;
            }
        }

        private void Query()
        {
            // include half the added items in the query
            int notFound = 0, truePositive = 0, falsePositive = 0;

            long i = count / 2;

            while (i < count + count / 2)
            {
                if (!_filter.Contains(i))
                {
                    notFound++;
                    //Console.WriteLine($"{i} not found");
                }
                else
                {
                    if (i < count)
                    {
                        truePositive++;
                        //Console.WriteLine($"{i} found (true positive)");
                    }
                    else
                    {
                        falsePositive++;
                        //Console.WriteLine($"{i} not found (false positive)");
                    }
                }
                i++;
            }

            Console.WriteLine($"Total items queried {count}, Found: {truePositive} NotFound: {notFound + falsePositive} with {falsePositive} false positives ({double.Round(falsePositive / ((double)(notFound + falsePositive)), 6)})");
        }
    }
}