using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DBBranchManager.Caching
{
    internal class HashTransformer : IDisposable
    {
        private readonly StateHash mHash;
        private readonly HashAlgorithm mHasher;
        private readonly CryptoStream mCryptoStream;

        public HashTransformer(StateHash hash)
        {
            mHash = hash;
            if (hash == null)
                return;

            mHasher = new SHA256Cng();

            var bytes = mHash.GetBytes();
            mHasher.TransformBlock(bytes, 0, bytes.Length, null, 0);

            mCryptoStream = new CryptoStream(new FakeStream(), mHasher, CryptoStreamMode.Write);
        }

        public void Dispose()
        {
            if (mHash == null)
                return;

            mCryptoStream.Close();
        }

        public void Transform(Stream stream)
        {
            if (mHash == null)
                return;

            stream.CopyTo(mCryptoStream);
        }

        public void Transform(string str)
        {
            if (mHash == null)
                return;

            var bytes = Encoding.UTF8.GetBytes(str);
            mCryptoStream.Write(bytes, 0, bytes.Length);
        }

        public StateHash GetResult()
        {
            if (mHash == null)
                return null;

            mCryptoStream.Close();
            return new StateHash(mHasher.Hash);
        }

        private class FakeStream : Stream
        {
            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { return 0; }
            }

            public override long Position
            {
                get { return 0; }
                set { }
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }
        }
    }
}
