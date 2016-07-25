using System.Security;

namespace DBBranchManager.Utils
{
    internal static class Extensions
    {
        public static SecureString ToSecureString(this string str)
        {
            var secure = new SecureString();
            foreach (var c in str)
            {
                secure.AppendChar(c);
            }
            return secure;
        }
    }
}