using System;

namespace DBBranchManager.Exceptions
{
    internal class SoftFailureException : Exception
    {
        public SoftFailureException()
        {
        }

        public SoftFailureException(string message) :
            base(message)
        {
        }

        public SoftFailureException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
