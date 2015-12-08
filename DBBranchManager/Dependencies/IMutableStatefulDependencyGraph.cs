namespace DBBranchManager.Dependencies
{
    internal interface IMutableStatefulDependencyGraph<T> : IMutableDependencyGraph<T>, IStatefulDependencyGraph<T>
    {
    }
}