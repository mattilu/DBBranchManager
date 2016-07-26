using System;
using System.Runtime.InteropServices;
using System.Security;
using DBBranchManager.Logging;
using DBBranchManager.Tasks;

namespace DBBranchManager.Utils
{
    internal static class MiscExtensions
    {
        public static string ToUnsecureString(this SecureString value)
        {
            var valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static SecureString ToSecureString(this string str)
        {
            var secure = new SecureString();
            foreach (var c in str)
            {
                secure.AppendChar(c);
            }
            return secure;
        }

        public static IDisposable IndentScope(this TaskExecutionContext context)
        {
            return new IndentScopeImpl(context.Log);
        }

        public static IDisposable IndentScope(this ILog log)
        {
            return new IndentScopeImpl(log);
        }

        private class IndentScopeImpl : IDisposable
        {
            private readonly ILog mLog;

            public IndentScopeImpl(ILog log)
            {
                mLog = log;
                log.Indent();
            }

            public void Dispose()
            {
                mLog.UnIndent();
            }
        }
    }
}