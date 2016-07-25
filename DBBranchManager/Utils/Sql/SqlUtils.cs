using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using DBBranchManager.Entities;
using DBBranchManager.Entities.Config;

namespace DBBranchManager.Utils.Sql
{
    internal static class SqlUtils
    {
        public static string ToBracketedIdentifier(string identifier)
        {
            return string.Format("[{0}]", identifier.Replace("]", "]]"));
        }

        public static IProcessExecutionResult SqlCmdExec(DatabaseConnectionConfig databaseConnection, string script)
        {
            var args = string.Format(@"-r 1 -S ""{0}"" -U ""{1}"" -P ""{2}""", databaseConnection.Server, databaseConnection.User, databaseConnection.Password.ToUnsecureString());
            var input = new MemoryStream();
            var writer = new StreamWriter(input);
            writer.WriteLine(script);
            writer.WriteLine("GO");
            writer.WriteLine("exit");
            writer.Flush();

            input.Position = 0;

            return ProcessUtils.Exec("sqlcmd", args, input);
        }

        public static IProcessExecutionResult Exec(DatabaseConnectionConfig databaseConnection, string dbName, string script, IEnumerable<SqlParameter> parameters)
        {
            return new SqlCommandExecutionResult(databaseConnection, dbName, script, parameters);
        }

        public static IReadOnlyCollection<Tuple<string, string>> GetLogicalAndPhysicalNamesFromBackupFile(DatabaseConnectionConfig databaseConnection, string backupFile)
        {
            using (var f = new SqlCommandFactory(ToConnectionString(databaseConnection), null))
            using (var cmd = f.CreateCommand(null))
            {
                cmd.CommandText = "RESTORE FILELISTONLY FROM DISK = @path";
                cmd.Parameters.Add("@path", SqlDbType.NVarChar).Value = backupFile;

                var result = new List<Tuple<string, string>>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var logicalName = (string)r["LogicalName"];
                        var physicalName = (string)r["PhysicalName"];

                        result.Add(Tuple.Create(logicalName, physicalName));
                    }
                }

                return result;
            }
        }

        private static string ToConnectionString(DatabaseConnectionConfig databaseConnection)
        {
            var csb = new SqlConnectionStringBuilder
            {
                DataSource = databaseConnection.Server,
                UserID = databaseConnection.User,
                Password = databaseConnection.Password.ToUnsecureString()
            };

            return csb.ToString();
        }

        private class SqlCommandExecutionResult : IProcessExecutionResult
        {
            private readonly SqlCommandFactory mFactory;
            private readonly SqlCommand mCommand;
            private readonly BlockingCollection<ProcessOutputLine> mOutput;
            private bool mGotErrors;
            private bool mDisposed;

            public SqlCommandExecutionResult(DatabaseConnectionConfig db, string name, string script, IEnumerable<SqlParameter> parameters)
            {
                mFactory = new SqlCommandFactory(ToConnectionString(db), OnMessage);
                mCommand = mFactory.CreateCommand(name);
                mCommand.CommandText = script;
                mOutput = new BlockingCollection<ProcessOutputLine>();

                foreach (var sqlParameter in parameters)
                {
                    mCommand.Parameters.Add(sqlParameter);
                }
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