﻿using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using HashDepot;

namespace Wibblr.Collections.BloomFilter
{
    public class BloomFilter<T>(int capacity, double falsePositiveRatio) : BloomFilter(capacity, falsePositiveRatio)
        where T : unmanaged
    {
        public void Add(T item)
        {
            Add(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref item, 1)));
        }

        public bool Contains(T item)
        {
            return Contains(MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref item, 1)));
        }
    }

    public class BloomFilter
    {
        protected enum Operation
        {
            Add = 0,
            Query = 1
        };

        private readonly BitArray _storage;
        public int RequestedCapacity { get; }
        public double RequestedFalsePositiveRatio { get; }
        public int FilterSize { get; }
        public int HashCount { get; }
        public int HashLength { get; }
        public ulong Seed { get; }
        public double CapacityAtRequestedFalsePositiveRatio { get; }
        public double FalsePositiveRatioAtRequestedCapacity { get; }

        public BloomFilter(int capacity, double falsePositiveRatio)
        {
            RequestedCapacity = capacity;
            RequestedFalsePositiveRatio = falsePositiveRatio;
            FilterSize = BloomFilterUtils.CalculateFilterSize(capacity, falsePositiveRatio);
            (HashCount, FalsePositiveRatioAtRequestedCapacity) = BloomFilterUtils.CalculateHashCount(FilterSize, capacity);
            HashLength = BitOperations.TrailingZeroCount(FilterSize); // relies on the filter size being a power of 2;
            Seed = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
            CapacityAtRequestedFalsePositiveRatio = BloomFilterUtils.CalculateCapacity(FilterSize, HashCount, falsePositiveRatio);

            _storage = new BitArray(FilterSize);
        }

        private bool AddOrQuery(ReadOnlySpan<byte> source, Operation operation)
        {
            var mask = (1 << HashLength) - 1;
            var seed = Seed;
            var hash64 = 0ul;
            var hashBitsAvailable = 0;

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
                    _storage[hash] = true;
                }
                else if (operation == Operation.Query)
                {
                    if (!_storage[hash])
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

        public bool Contains(ReadOnlySpan<byte> buf)
        {
            return AddOrQuery(buf, Operation.Query);
        }
    }
}