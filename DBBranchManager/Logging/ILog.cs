namespace DBBranchManager.Logging
{
    internal interface ILog
    {
        void Log(object obj);
        void LogFormat(string format, params object[] args);

        void Indent();
        void UnIndent();
    }
}
