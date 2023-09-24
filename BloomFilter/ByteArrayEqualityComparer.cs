using System.Diagnostics.CodeAnalysis;

namespace BloomFilter.Core
{
    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            //if (x == null)
            //{
            //    return y == null;
            //}
            //if (y == null)
            //{
            //    return x == null;
            //}

            return x.SequenceEqual(y);
        }

        public int GetHashCode([DisallowNull] byte[] obj)
        {
            throw new NotImplementedException();
        }
    }
}