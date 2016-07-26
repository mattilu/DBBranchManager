using System;

namespace DBBranchManager.Tasks
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class DontRegisterAttribute : Attribute
    {
    }
}
