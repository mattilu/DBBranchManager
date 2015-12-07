namespace DBBranchManager.Activators
{
    internal delegate void ActivateDelegate();

    internal interface IActivator
    {
        void Start();

        void Stop();

        event ActivateDelegate Activate;
    }
}