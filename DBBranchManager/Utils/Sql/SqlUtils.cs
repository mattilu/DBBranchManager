using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using DBBranchManager.Config;

namespace DBBranchManager.Utils.Sql
{
    internal static class SqlUtils
    {
        public static IProcessExecutionResult SqlCmdExec(DatabaseConnectionInfo db, string script)
        {
            var args = string.Format(@"-r 1 -S ""{0}"" -U ""{1}"" -P ""{2}""", db.Server, db.User, db.Password);
            var input = new MemoryStream();
            var writer = new StreamWriter(input);
            writer.WriteLine(script);
            writer.WriteLine("GO");
            writer.WriteLine("exit");
            writer.Flush();

            input.Position = 0;

            return ProcessUtils.Exec("sqlcmd", args, input);
        }

        public static IProcessExecutionResult Exec(DatabaseConnectionInfo db, string script)
        {
            var csb = new SqlConnectionStringBuilder
            {
                DataSource = db.Server,
                UserID = db.User,
                Password = db.Password
            };

            return new SqlCommandExecutionResult(csb.ToString(), db.Name, script);
        }

        private class SqlCommandExecutionResult : IProcessExecutionResult
        {
            private readonly SqlCommandFactory mFactory;
            private readonly SqlCommand mCommand;
            private readonly BlockingCollection<ProcessOutputLine> mOutput;
            private bool mGotErrors;
            private bool mDisposed;

            public SqlCommandExecutionResult(string connectionString, string name, string script)
            {
                mFactory = new SqlCommandFactory(connectionString, OnMessage);
                mCommand = mFactory.CreateCommand(name);
                mCommand.CommandText = script;
                mOutput = new BlockingCollection<ProcessOutputLine>();
            }

            private void OnMessage(object sender, SqlMessageEventArgs e)
            {
                foreach (SqlError error in e.Errors)
                {
                    ProcessOutputLine.OutputTypeEnum type;
                    if (error.Class > 10)
                    {
                        type = ProcessOutputLine.OutputTypeEnum.StandardError;
                        mGotErrors = true;
                    }
                    else
                    {
                        type = ProcessOutputLine.OutputTypeEnum.StandardOutput;
                    }

                    mOutput.Add(new ProcessOutputLine(type, error.Message));
                }
            }

            #region IProcessExecutionResult

            public IEnumerable<ProcessOutputLine> GetOutput()
            {
                var executeTask = mCommand.ExecuteNonQueryAsync()
                    .ContinueWith(_ => mOutput.CompleteAdding());

                foreach (var outputLine in mOutput.GetConsumingEnumerable())
                {
                    yield return outputLine;
                }

                executeTask.Wait();
            }

            public int ExitCode
            {
                get { return mGotErrors ? 1 : 0; }
            }

            #endregion

            #region IDisposable

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (mDisposed)
                    return;

                if (disposing)
                {
                    mCommand.Dispose();
                    mFactory.Dispose();
                    mOutput.Dispose();
                }

                mDisposed = true;
            }

            #endregion
        }
    }
}