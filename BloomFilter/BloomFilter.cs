using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using HashDepot;

namespace BloomFilter
{
    public class BloomFilter
    {
        private enum Operation
        {
            Add = 0,
            Query = 1
        };

        private readonly BitArray _value;
        private readonly int _hashCount;
        private readonly int _hashLength;
        private readonly ulong _seed;

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
        private static double CalculateCapacity(double m, double k, double p)
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
        private static int CalculateFilterSize(double n, double p)
        {
            const int maxSize = 1 << 30;

            if (p <= 0 || p >= 1)
            {
                throw new ArgumentException(nameof(p));
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
        /// <param name="actualFalsePositiveRatio">Returns the actual false positive ratio of the returned hash count</param>
        /// <returns>The hash function count</returns>
        private static int CalculateHashCount(int m, double n, out double actualFalsePositiveRatio)
        {
            // Calculate the value rounded both up and down, calculate p for each,
            // and take the value which gives the lower value of p
            int k = (int)(m / n * Math.Log(2));

            double p1 = CalculateFalsePositiveRatio(k, m, n);
            double p2 = CalculateFalsePositiveRatio(k + 1, m, n);

            if (p1 < p2)
            {
                actualFalsePositiveRatio = p1;
                return k;
            }

            actualFalsePositiveRatio = p2;
            return k + 1;
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
        private static double CalculateFalsePositiveRatio(double k, double m, double n)
        {
            return Math.Pow(1 - Math.Exp(-k / (m / n)), k);
        }

        public BloomFilter(int capacity, double falsePositiveRatio)
        {
            int filterSize = CalculateFilterSize(capacity, falsePositiveRatio);
            
            _hashCount = CalculateHashCount(filterSize, capacity, out var actualFalsePositiveRatio);
            _hashLength = BitOperations.TrailingZeroCount(filterSize); // relies on the filter size being a power of 2
            _value = new BitArray(filterSize);
            _seed = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));

            Console.WriteLine($"Creating bloom filter with capacity {capacity} and false positive ratio under {falsePositiveRatio}."); 
            Console.WriteLine($"  Allocating filter size={filterSize} bits ({filterSize / 8192} KB); hash count={_hashCount}; hash length={_hashLength} bits");

            var capacityAtExpectedFalsePositiveRatio = CalculateCapacity(filterSize, _hashCount, falsePositiveRatio);
            Console.WriteLine($"  Expected capacity={capacityAtExpectedFalsePositiveRatio} at false positive ratio={double.Round(falsePositiveRatio, 6)}");
            Console.WriteLine($"  Expected false positive ratio={double.Round(actualFalsePositiveRatio, 6)} at capacity={capacity}");
        }

        private bool AddOrQuery(ReadOnlySpan<byte> source, Operation operation)
        {
            var mask = (1 << _hashLength) - 1;
            var seed = _seed;
            var hash64 = 0ul;
            var hashBitsAvailable = 0;

            for (int i = 0; i < _hashCount; i++)
            {
                if (hashBitsAvailable < _hashLength)
                {
                    hash64 = XXHash.Hash64(source, seed++);
                    hashBitsAvailable = 64;
                }

                int hash = (int)hash64 & mask;
                hashBitsAvailable -= _hashLength;
                hash64 >>= _hashLength;

                if (operation == Operation.Add)
                {
                    _value[hash] = true;
                }
                else if (operation == Operation.Query)
                {
                    if (!_value[hash])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void Add(ReadOnlySpan<byte> buf)
        {
            AddOrQuery(buf, Operation.Add);
        }

        public void Add<T>(T item) where T : unmanaged
        {
            Add(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref item, 1)));
        }

        public bool MayContain(ReadOnlySpan<byte> buf)
        {
            return AddOrQuery(buf, Operation.Query);
        }

        public bool MayContain<T>(T item) where T : unmanaged
        {
            return MayContain(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref item, 1)));
        }
    }
}