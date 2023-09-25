using System.Text;
using BloomFilter.Core;
namespace BloomFilter.Demo
{
    internal class Program
    {
        BloomFilter _filter;

        static async Task Main(string[] args)
        {
            var count = 20000;

            var words = GetUniqueWords(await GetBibleText());

            var words2 = Enumerable.Range(0, count * 2)
                .Select(x => Encoding.UTF8.GetBytes(x.ToString()))
                .ToList();

            new Program().Run(words2, count, 0.1d);
        }

        // Use this for memory profiling
        static void Main2(string[] args)
        {
            var x = new BloomFilter(10000, 0.1d);

            x.Add(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        static async Task<string> GetBibleText()
        {
            string localFilePath = "kjv.txt";
            try
            {
                return File.ReadAllText(localFilePath);
            }
            catch (Exception)
            {
                Console.WriteLine("Downloading..");
            }

            try
            {
                string url = "https://www.gutenberg.org/cache/epub/10/pg10.txt";

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = File.Create(localFilePath))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return File.ReadAllText(localFilePath);
        }

        public static List<byte[]> GetUniqueWords(string text)
        {
            var words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet()
                .Select(Encoding.UTF8.GetBytes)
                .ToList();

            return words;
        }

        public void Run(List<byte[]> words, int itemsToAdd, double p)
        {
            _filter = new BloomFilter(itemsToAdd, p);

            var addedItems = words.Take(itemsToAdd).ToList();
            AddItems(words.Take(itemsToAdd));

            // include half the added words in the query
            var queriedItems = words.Skip(itemsToAdd / 2).Take(itemsToAdd).ToList();
            QueryItems(addedItems, queriedItems);
        }

        public void AddItems(IEnumerable<byte[]> items)
        {
            foreach (var word in items)
            {
                _filter.Add(word);
            }
        }

        public void QueryItems(IList<byte[]> addedItems, IList<byte[]> queriedItems)
        {
            int notFound = 0, truePositive = 0, falsePositive = 0;
            var comparer = new ByteArrayEqualityComparer();

            foreach (var word in queriedItems)
            {
                if (!_filter.MayContain(word))
                {
                    notFound++;
                   // Console.WriteLine($"{Encoding.UTF8.GetString(word)} not found");
                }
                else
                {
                    if (addedItems.Contains(word, comparer))
                    {
                        truePositive++;
                        //Console.WriteLine($"{Encoding.UTF8.GetString(word)} found (true positive)");
                    }
                    else
                    {
                        falsePositive++;
                        //Console.WriteLine($"{Encoding.UTF8.GetString(word)} not found (false positive)");
                    }
                }
            }

            Console.WriteLine($"Total {queriedItems.Count}, Found: {truePositive} NotFound: {notFound + falsePositive} with {falsePositive} false positives ({falsePositive / (double)(notFound + falsePositive)})");
        }
    }
}