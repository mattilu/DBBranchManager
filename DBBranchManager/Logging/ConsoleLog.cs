using System;

namespace DBBranchManager.Logging
{
    internal class ConsoleLog : ILog
    {
        private int mIndentation;

        public void Log(object obj)
        {
            LogString(obj.ToString());
        }

        public void LogFormat(string format, params object[] args)
        {
            LogString(string.Format(format, args));
        }

        public void Indent()
        {
            ++mIndentation;
        }

        public void UnIndent()
        {
            --mIndentation;
        }

        private void LogString(string str)
        {
            var lines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var header = string.Format("[{0:T}] {1}", DateTime.Now, new string(' ', mIndentation * 2));

            foreach (var line in lines)
            {
                LogLine(header, line);
            }
        }

        private void LogLine(string header, string line)
        {
            Console.WriteLine("{0}{1}", header, line);
        }
    }
}