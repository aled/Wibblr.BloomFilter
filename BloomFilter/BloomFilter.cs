using System.Collections;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace BloomFilter
{
    class BloomFilter
    {
        private readonly BitArray _value;
        private int _hash16Count;
        private long _seed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialSize">Actual number of bits is 2^initialSize</param>
        public BloomFilter(int initialSize, int hashCount)
        {
            if (initialSize < 8 || initialSize > 16)
            {
                throw new ArgumentException("initialSize must be between 8 (for 256 bits) and 16 (for 65536 bits)");
            }

            if (hashCount < 1 || hashCount > 16)
            {
                throw new ArgumentException("hashCount must be between 1 and 16");
            }

            _value = new BitArray(1 << initialSize);
            _hash16Count = hashCount;
            _seed = BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8));
        }

        private byte[] GetHashBytes(ReadOnlySpan<byte> buf)
        { 
            // Each of the 64-bit hashes is treated as 4 separate 16-bit hashes.
            // At most there will be 4 64-bit hashes to create 16 16-bit hashes
            var hash64Count = _hash16Count / 4;
            var hashByteCount = hash64Count * 8;
            var hashBytes = new byte[hashByteCount];

            for (int i = 0; i < hashByteCount; i += 8)
            {
                XxHash64.Hash(buf, hashBytes.AsSpan(i), _seed + i);
            }

            return hashBytes;
        }

        public void Add(ReadOnlySpan<byte> buf)
        {
            var hashBytes = GetHashBytes(buf);

            for (int i = 0; i < _hash16Count; i += 2)
            {
                int index = BitConverter.ToUInt16(hashBytes, i) % _value.Length; // length is a power of 2 so there will be no remainder
                _value[index] = true;
            }   
        }

        public bool MayContain(ReadOnlySpan<byte> buf)
        {
            var hashBytes = GetHashBytes(buf);

            for (int i = 0; i < _hash16Count; i += 2)
            {
                int index = BitConverter.ToUInt16(hashBytes, i) % _value.Length; // length is a power of 2 so there will be no remainder
                if (_value[index] == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}