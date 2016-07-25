namespace DBBranchManager.Entities.Config
{
    internal class BeepConfig
    {
        private readonly int mFrequency;
        private readonly int mDuration;
        private readonly int mPulses;
        private readonly double mDutyCycle;

        public BeepConfig(int frequency, int duration, int pulses, double dutyCycle)
        {
            mFrequency = frequency;
            mDuration = duration;
            mPulses = pulses;
            mDutyCycle = dutyCycle;
        }

        public int Frequency
        {
            get { return mFrequency; }
        }

        public int Duration
        {
            get { return mDuration; }
        }

        public int Pulses
        {
            get { return mPulses; }
        }

        public double DutyCycle
        {
            get { return mDutyCycle; }
        }
    }
}