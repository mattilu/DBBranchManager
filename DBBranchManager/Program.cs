using System;
using System.IO;
using DBBranchManager.Exceptions;
using Mono.Options;

namespace DBBranchManager
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var app = new Application();
                return app.Run(args);
            }
            catch (SoftFailureException ex)
            {
                DumpException(Console.Error, ex, true);
                return 1;
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled {0}", ex.GetType());
                DumpException(Console.Error, ex, false);
                return 2;
            }
        }

        private static void DumpException(TextWriter writer, Exception ex, bool messageOnly)
        {
            var indent = 0;
            DumpOne(writer, ex, messageOnly, false, indent);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                indent += 2;
                DumpOne(writer, ex, messageOnly, true, indent);
            }
        }

        private static void DumpOne(TextWriter writer, Exception ex, bool messageOnly, bool inner, int indent)
        {
            var indentStr = new string(' ', indent);
            if (inner && !messageOnly)
                writer.WriteLine("{0}Inner Exception:", indentStr);
            writer.WriteLine("{0}{1}", indentStr, ex.Message);

            if (!messageOnly)
            {
                writer.WriteLine("{0} Stack Trace:", indentStr);
                foreach (var line in ex.StackTrace.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                {
                    writer.WriteLine("{0} {1}", indentStr, line);
                }
            }
        }
    }
}