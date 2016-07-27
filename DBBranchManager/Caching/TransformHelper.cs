using System;
using System.IO;

namespace DBBranchManager.Caching
{
    internal static class TransformHelper
    {
        private const int FileSizeLimit = 10 * 1024 * 1024;

        public static void TransformWithFileSmart(this HashTransformer transformer, string filePath)
        {
            var file = new FileInfo(filePath);
            if (!file.Exists)
                throw new FileNotFoundException(string.Format("Cannot find file {0}", filePath), filePath);

            if (file.Length > FileSizeLimit)
            {
                transformer.Transform(string.Format("{0}:{1}", file.Length, file.LastWriteTimeUtc.Ticks));
                using (var fs = file.OpenRead())
                using (var sub = new SkippingSubStream(fs, file.Length))
                {
                    transformer.Transform(sub);
                }
            }
            else
            {
                using (var fs = file.OpenRead())
                {
                    transformer.Transform(fs);
                }
            }
        }

        private class SkippingSubStream : Stream
        {
            private const int NumChunks = 10240;
            private const int ChunkSize = 1024;

            private readonly Stream mUnderlyingStream;
            private readonly long mLength;
            private int mChunk;
            private int mOffset;

            public SkippingSubStream(Stream underlyingStream, long lengh)
            {
                mUnderlyingStream = underlyingStream;
                mLength = lengh;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override long Length
            {
                get { return ChunkSize * NumChunks; }
            }

            public override long Position
            {
                get { return mChunk * ChunkSize + mOffset; }
                set { throw new NotSupportedException(); }
            }

            public override void Flush()
            {
                mUnderlyingStream.Flush();
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
                var total = 0;

                while (count > 0)
                {
                    var left = MaybeAdvanceChunk();
                    var wanted = Math.Min(left, count);

                    var read = mUnderlyingStream.Read(buffer, offset, wanted);
                    if (read == 0)
                        return total;

                    mOffset += read;
                    offset += read;
                    total += read;
                    count -= read;
                }

                return total;
            }

            private int MaybeAdvanceChunk()
            {
                if (mChunk >= NumChunks)
                    return 0;

                if (mOffset < ChunkSize)
                    return ChunkSize - mOffset;

                mOffset = 0;
                ++mChunk;

                if (mChunk >= NumChunks)
                    return 0;

                if (mChunk == NumChunks - 1)
                    mUnderlyingStream.Seek(-ChunkSize, SeekOrigin.End);
                else
                    mUnderlyingStream.Seek((mLength - ChunkSize) * mChunk / (NumChunks - 1), SeekOrigin.Begin);

                return ChunkSize;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
