using System;

namespace DBBranchManager.Caching
{
    internal class StateHash
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

        public byte[] GetBytes()
        {
            return (byte[])mHash.Clone();
        }
    }
}