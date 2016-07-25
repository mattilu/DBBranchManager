using System;
using System.Threading;

namespace DBBranchManager.Utils
{
    internal class Buzzer
    {
        public static void Beep(int frequency, int duration, int times, double dutyTime)
        {
            var time = (double)duration / times;
            var onTime = (int)(time * dutyTime);
            var offTime = (int)(time - onTime);

            while (times-- > 0)
            {
                Console.Beep(frequency, onTime);
                Thread.Sleep(offTime);
            }
        }
    }
}