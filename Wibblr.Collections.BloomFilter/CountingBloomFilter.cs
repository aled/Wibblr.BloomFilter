using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using HashDepot;

namespace Wibblr.Collections.BloomFilter
{
    public class CountingBloomFilter<T>(int capacity, double falsePositiveRatio) : CountingBloomFilter(capacity, falsePositiveRatio)
        where T : unmanaged
    {
        public void Increment(T item, int count)
        {
            Increment(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref item, 1)), count);
        }

        public int Count(T item)
        {
            return Count(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref item, 1)));
        }
    }

    public class CountingBloomFilter
    {
        protected enum Operation
        {
            Add = 0,
            Query = 1
        };

        private readonly ushort[] _storage;
        public int RequestedCapacity { get; }
        public double RequestedFalsePositiveRatio { get; }
        public int FilterSize { get; }
        public int HashCount { get; }
        public int HashLength { get; }
        public ulong Seed { get; }
        public double CapacityAtRequestedFalsePositiveRatio { get; }
        public double FalsePositiveRatioAtRequestedCapacity { get; }

        public CountingBloomFilter(int capacity, double falsePositiveRatio)
        {
            RequestedCapacity = capacity;
            RequestedFalsePositiveRatio = falsePositiveRatio;
            FilterSize = BloomFilterUtils.CalculateFilterSize(capacity, falsePositiveRatio);
            (HashCount, FalsePositiveRatioAtRequestedCapacity) = BloomFilterUtils.CalculateHashCount(FilterSize, capacity);
            HashLength = BitOperations.TrailingZeroCount(FilterSize); // relies on the filter size being a power of 2;
            Seed = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
            CapacityAtRequestedFalsePositiveRatio = BloomFilterUtils.CalculateCapacity(FilterSize, HashCount, falsePositiveRatio);

            _storage = new ushort[FilterSize];
        }

        private int IncrementOrQuery(ReadOnlySpan<byte> source, Operation operation, int count = 1)
        {
            var mask = (1 << HashLength) - 1;
            var seed = Seed;
            var hash64 = 0ul;
            var hashBitsAvailable = 0;

            ushort min = ushort.MaxValue;

            for (int i = 0; i < HashCount; i++)
            {
                if (hashBitsAvailable < HashLength)
                {
                    hash64 = XXHash.Hash64(source, seed++);
                    hashBitsAvailable = 64;
                }

                int hash = (int)hash64 & mask;
                hashBitsAvailable -= HashLength;
                hash64 >>= HashLength;

                if (operation == Operation.Add)
                {
                    if (_storage[hash] + count - 1 < ushort.MaxValue)
                    {
                        _storage[hash] = (ushort)(_storage[hash] + count);
                    }
                    else
                    {
                        _storage[hash] = ushort.MaxValue;
                    }
                }
                else if (operation == Operation.Query)
                {
                    if (_storage[hash] < min)
                    {
                        min = _storage[hash];
                    }
                }
            }
            return min;
        }

        public void Increment(ReadOnlySpan<byte> buf, int count = 1)
        {
            IncrementOrQuery(buf, Operation.Add, count);
        }

        public int Count(ReadOnlySpan<byte> buf)
        {
            return IncrementOrQuery(buf, Operation.Query);
        }
    }
}