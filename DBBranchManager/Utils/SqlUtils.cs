using DBBranchManager.Config;
using System;
using System.IO;

namespace DBBranchManager.Utils
{
    internal class SqlCmdFailedException : Exception
    {
        private readonly string mMessages;

        public SqlCmdFailedException(string messages)
        {
            mMessages = messages;
        }

        public string Messages
        {
            get { return mMessages; }
        }
    }

    internal static class SqlUtils
    {
        public static void SqlCmdExec(DatabaseConnectionInfo db, string script)
        {
            var args = string.Format(@"-r 1 -S ""{0}"" -U ""{1}"" -P ""{2}""", db.Server, db.User, db.Password);
            using (var input = new MemoryStream())
            {
                var writer = new StreamWriter(input);
                writer.WriteLine(script);
                writer.WriteLine("GO");
                writer.WriteLine("exit");
                writer.Flush();

                input.Position = 0;

                var result = ProcessUtils.Exec("sqlcmd", args, input);
                if (result.ExitCode != 0)
                {
                    throw new SqlCmdFailedException(result.StandardError);
                }
            }
        }
    }
}