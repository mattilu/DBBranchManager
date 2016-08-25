using System;
using System.Threading;

namespace DBBranchManager.Utils
{
    internal static class Buzzer
    {
        public static void Beep(int frequency, int duration, int pulses, double dutyCycle)
        {
            var time = (double)duration / pulses;
            var onTime = (int)(time * dutyCycle);
            var offTime = (int)(time - onTime);

            while (pulses-- > 0)
            {
                Console.Beep(frequency, onTime);
                Thread.Sleep(offTime);
            }
        }
    }
}
