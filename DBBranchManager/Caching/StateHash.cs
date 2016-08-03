using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBBranchManager.Caching
{
    internal sealed class StateHash : IEquatable<StateHash>
    {
        private const int HashSize = 32;
        private static readonly StateHash EmptyHash = new StateHash();

        private readonly byte[] mHash;

        private StateHash()
        {
            mHash = new byte[HashSize];
        }

        internal StateHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            if (hash.Length != HashSize)
                throw new ArgumentException(string.Format("Array should be {0} bytes long", HashSize), "hash");

            mHash = hash;
        }

        public static StateHash Empty
        {
            get { return EmptyHash; }
        }

        public bool Equals(StateHash other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return mHash.SequenceEqual(other.mHash);
        }

        public static StateHash FromHexString(string hex)
        {
            if (hex.Length != HashSize * 2)
                throw new ArgumentOutOfRangeException("hex");

            var hash = new byte[HashSize];
            for (var i = 0; i < HashSize; ++i)
            {
                hash[i] = FromChars(hex[i * 2], hex[i * 2 + 1]);
            }

            return new StateHash(hash);
        }

        public string ToHexString()
        {
            return ByteArrayToHexString(mHash);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as StateHash);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const uint p = 16777619;
                var hash = 2166136261U;

                for (var i = 0; i < HashSize; ++i)
                    hash = (hash ^ mHash[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;

                return (int)hash;
            }
        }

        public static bool operator ==(StateHash left, StateHash right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StateHash left, StateHash right)
        {
            return !Equals(left, right);
        }

        public byte[] GetBytes()
        {
            return (byte[])mHash.Clone();
        }

        private static byte FromChars(char ch, char cl)
        {
            return (byte)(FromChar(ch) << 4 | FromChar(cl));
        }

        private static byte FromChar(char c)
        {
            if (c >= '0' && c <= '9')
                return (byte)(c - '0');
            if (c >= 'a' && c <= 'f')
                return (byte)(c - 'a' + 10);
            if (c >= 'A' && c <= 'F')
                return (byte)(c - 'A' + 10);

            throw new ArgumentOutOfRangeException("c");
        }

        private static string ByteArrayToHexString(IReadOnlyCollection<byte> bytes)
        {
            const string hexAlphabet = "0123456789ABCDEF";
            var result = new StringBuilder(bytes.Count * 2);

            foreach (var b in bytes)
            {
                result.Append(hexAlphabet[b >> 4]);
                result.Append(hexAlphabet[b & 0xF]);
            }

            return result.ToString();
        }
    }
}
