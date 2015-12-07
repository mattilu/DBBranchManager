using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace DBBranchManager.Utils
{
    public class ProcessExecutionResult
    {
        public ProcessExecutionResult(string standardOutput, string standardError, int exitCode)
        {
            StandardOutput = standardOutput;
            StandardError = standardError;
            ExitCode = exitCode;
        }

        public string StandardOutput { get; private set; }
        public string StandardError { get; private set; }
        public int ExitCode { get; private set; }
    }

    internal static class ProcessUtils
    {
        public static ProcessExecutionResult Exec(string file, string args, Stream input)
        {
            const int timeout = int.MaxValue;
            using (var p = new Process())
            {
                p.StartInfo = new ProcessStartInfo(file, args)
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var output = new StringBuilder();
                var error = new StringBuilder();

                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false))
                {
                    p.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                            //Console.WriteLine(e.Data);
                        }
                    };
                    p.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                            Console.WriteLine(e.Data);
                        }
                    };

                    p.Start();

                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    input.CopyTo(p.StandardInput.BaseStream);
                    p.StandardInput.BaseStream.Flush();

                    if (p.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        return new ProcessExecutionResult(output.ToString(), error.ToString(), p.ExitCode);
                    }
                    throw new TimeoutException("Process timed out");
                }
            }
        }
    }
}