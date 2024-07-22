using System.Numerics;

namespace Wibblr.Collections.BloomFilter
{
    public static class BloomFilterUtils
    {
        /// <summary>
        /// Calculate the capacity of the filter given the filter size, hash count and
        /// false positive ratio; using the formula
        ///
        ///   n = ceil(m / (-k / log(1 - exp(log(p) / k))))
        /// </summary>
        /// <param name="m">filter size in bits</param>
        /// <param name="k">number of hash functions</param>
        /// <param name="p">false positive ratio</param>
        /// <returns>The capacity of the filter</returns>
        public static double CalculateCapacity(double m, double k, double p)
        {
            return Math.Ceiling(m / (-k / Math.Log(1 - Math.Exp(Math.Log(p) / k))));
        }

        /// <summary>
        /// Calculate the filter size in bits, given the expected capacity and false positive ratio;
        /// using the formula
        ///
        ///   m = ceil((n * log(p)) / log(1 / pow(2, log(2))));
        ///
        /// Restrict length to a power of 2 to avoid having to do rejection sampling on the hashes;
        /// BitArray length is limited to int.MaxValue (2^31 - 1), so the max size of the filter is
        /// actually 2^30 bits. (2^27 bytes, or 128MB)
        /// </summary>
        /// <param name="n">Expected filter capacity</param>
        /// <param name="p">Expected false positive ratio (greater than 0; less than 1)</param>
        /// <returns>The size of the filter in bits</returns>
        public static int CalculateFilterSize(double n, double p)
        {
            const int maxSize = 1 << 30;

            if (p <= 0 || p >= 1)
            {
                throw new ArgumentException("Invalid false positive ratio", nameof(p));
            }

            double m = n * Math.Log(p) / Math.Log(1 / Math.Pow(2, Math.Log(2)));

            return m > maxSize
                ? maxSize
                : (int)BitOperations.RoundUpToPowerOf2((uint)m);
        }

        /// <summary>
        /// Calculate the hash count, given the expected capacity and false positive ratio;
        /// using the formula:
        ///
        ///   k = round((m / n) * log(2));
        /// </summary>
        /// <param name="m">Filter size in bits</param>
        /// <param name="n">Capacity of the filter</param>
        /// <returns>The hash function count, and the actual false positive ratio of the returned hash count</returns>
        public static (int, double) CalculateHashCount(int m, double n)
        {
            // Calculate the value rounded both up and down, calculate p for each,
            // and take the value which gives the lower value of p
            int k = (int)(m / n * Math.Log(2));

            double p1 = CalculateFalsePositiveRatio(k, m, n);
            double p2 = CalculateFalsePositiveRatio(k + 1, m, n);

            return (p1 < p2) ? (k, p1) : (k + 1, p2);
        }

        /// <summary>
        /// Calculate the false positive ratio of the filter, given the hash count, filter size, and capacity;
        /// using the formula:
        ///
        ///   p = pow(1 - exp(-k / (m / n)), k)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <returns>False positive ratio</returns>
        public static double CalculateFalsePositiveRatio(double k, double m, double n)
        {
            return Math.Pow(1 - Math.Exp(-k / (m / n)), k);
        }
    }
}