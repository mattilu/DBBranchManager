using DBBranchManager.Config;
using DBBranchManager.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DBBranchManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = Configuration.LoadFromJson(@"..\..\config.json");
            var app = new Application(config);
            app.Run();
        }

        private static void BenchmarkNaturalSortComparer()
        {
            var n = 0;
            var avg = 0.0;
            var sqs = 0.0;
            var min = double.MaxValue;
            var max = double.MinValue;

            while (Console.ReadKey(true).KeyChar != 'q')
            {
                var rand = new Random();
                var toSort = (
                    from s1 in new string[] { "", "a", "b" }
                    from d1 in new[] { "00", "0", "01", "1", "10", "11" }
                    from s2 in new string[] { "", "x", "y" }
                    from d2 in new[] { "00", "0", "01", "1", "10", "11" }
                    from r in Enumerable.Range(0, 200).Select(x => rand.Next(1000))
                    select string.Format("{0}{1}{2}{4}_{3}", s1, d1, s2, d2, r)
                    ).OrderBy(x => x, Comparer<string>.Create((x, y) => rand.Next(-1, 1)))
                    .ToList();

                //var toSort = Directory.EnumerateFiles(@"D:\S2");

                var sw = new Stopwatch();

                sw.Start();
                var sorted1 = toSort.OrderBy(x => x, new NaturalSortComparer()).ToList();
                sw.Stop();
                var time1 = sw.Elapsed;
                sw.Restart();
                var sorted2 = toSort.OrderBy(x => x, new AlphanumComparator()).ToList();
                sw.Stop();
                var time2 = sw.Elapsed;

                var spd = GetSpeedup(time1, time2);
                avg += spd;
                sqs += spd * spd;

                if (spd < min)
                    min = spd;
                if (spd > max)
                    max = spd;

                Console.WriteLine("Method 1: {0}\nMethod 2: {1}\nSpeedup: {2:+#.##;-#.##;+0}%", time1, time2, spd);
                ++n;
                //File.WriteAllLines(@"E:\1.txt", sorted1);
                //File.WriteAllLines(@"E:\2.txt", sorted2);
            }

            Console.WriteLine("Min: {0:+#.##;-#.##;+0}, Max: {1:+#.##;-#.##;+0}, Avg: {2:+#.##;-#.##;+0}, StdDev: {3:+#.##;-#.##;+0}", min, max, avg / n, Math.Sqrt(n * sqs - avg * avg) / n);
        }

        private static double GetSpeedup(TimeSpan fastTime, TimeSpan slowTime)
        {
            var t1 = (double)fastTime.Ticks;
            var t2 = (double)slowTime.Ticks;

            return (t2 / t1 - 1) * 100;
        }
    }
}