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
            }
            else
            {
                using (var fs = file.OpenRead())
                {
                    transformer.Transform(fs);
                }
            }
        }
    }
}
