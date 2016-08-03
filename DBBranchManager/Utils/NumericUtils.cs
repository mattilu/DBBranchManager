using System.Text.RegularExpressions;

namespace DBBranchManager.Utils
{
    internal static class NumericUtils
    {
        public static long? TryParseByteSize(string str)
        {
            long result;
            return TryParseByteSize(str, out result) ? result : (long?)null;
        }

        public static bool TryParseByteSize(string str, out long result)
        {
            result = 0;

            if (str == null)
                return false;

            var match = Regex.Match(str, @"^(?<num>\d+)(?<unit>[KMGT])?$");
            if (!match.Success)
                return false;

            if (!long.TryParse(match.Groups["num"].Value, out result))
                return false;

            if (match.Groups["unit"].Success)
            {
                switch (match.Groups["unit"].Value)
                {
                    case "K":
                        result <<= 10;
                        break;

                    case "M":
                        result <<= 20;
                        break;

                    case "G":
                        result <<= 30;
                        break;

                    case "T":
                        result <<= 40;
                        break;
                }
            }

            return true;
        }
    }
}
