namespace BloomFilter.Demo
{
    internal class Program
    {
        int count = 200000;
        double falsePositiveRatio = 0.1d;

        BloomFilter _filter;

        static void Main(string[] args)
        {
            new Program().Run();
        }

        // Use this for memory profiling
        static void Main2(string[] args)
        {
            var x = new BloomFilter(10000, 0.1d);

            while (true)
            {
                x.Add(100L);
                Thread.Sleep(1000);
            }
        }

        public void Run()
        {
            _filter = new BloomFilter(count, falsePositiveRatio);

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
                if (!_filter.MayContain(i))
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