using System.Text;

namespace BloomFilter.Core
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await new Program().IndexFile();
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

        public async Task IndexFile()
        {
            var b = new BloomFilter(16, 8);

            var text = await GetBibleText();

            var words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet().Select(x => Encoding.UTF8.GetBytes(x)).ToList();

            var inputWords = words.Skip(0).Take(10000).ToArray();

            foreach (var word in inputWords)
            {
                b.Add(word);
            }

            var testWords = words.Skip(5000).Take(10000).ToArray(); ;

            int notFound = 0, truePositive = 0, falsePositive = 0;

            foreach (var word in testWords)
            {
                if (!b.MayContain(word))
                {
                    notFound++;
                    //Console.WriteLine($"{Encoding.UTF8.GetString(word)} not found");
                }
                else
                {
                    if (inputWords.Contains(word, new ByteArrayEqualityComparer()))
                    {
                        truePositive++;
                        //Console.WriteLine($"{Encoding.UTF8.GetString(word)} found (true positive)");
                    }
                    else
                    {
                        falsePositive++;
                        Console.WriteLine($"{Encoding.UTF8.GetString(word)} not found (false positive)");
                    }
                }
            }

            Console.WriteLine($"NotFound: {notFound} Found: {truePositive} FalsePositive: {falsePositive}");
        }
    }
}