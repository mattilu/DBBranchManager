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
            StateHash result;
            if (!TryFromHexString(hex, out result))
                throw new ArgumentOutOfRangeException("hex");

            return result;
        }

        public static bool TryFromHexString(string hex, out StateHash stateHash)
        {
            stateHash = null;
            if (hex.Length != HashSize * 2)
                return false;

            var hash = new byte[HashSize];
            for (var i = 0; i < HashSize; ++i)
            {
                if (!TryFromChars(hex[i * 2], hex[i * 2 + 1], out hash[i]))
                    return false;
            }

            stateHash = new StateHash(hash);
            return true;
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

        private static bool TryFromChars(char ch, char cl, out byte result)
        {
            byte h, l;
            if (!(TryFromChar(ch, out h) && TryFromChar(cl, out l)))
            {
                result = 0;
                return false;
            }

            result = (byte)(h << 4 | l);
            return true;
        }

        private static bool TryFromChar(char c, out byte result)
        {
            if (c >= '0' && c <= '9')
                result = (byte)(c - '0');
            else if (c >= 'a' && c <= 'f')
                result = (byte)(c - 'a' + 10);
            else if (c >= 'A' && c <= 'F')
                result = (byte)(c - 'A' + 10);
            else
            {
                result = 0;
                return false;
            }

            return true;
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
