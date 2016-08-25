using System;
using System.Collections.Generic;

namespace DBBranchManager.Utils
{
    internal class NaturalSortComparer : IComparer<string>
    {
        private static void Advance(string x, ref int ix, ref char cx, ref bool ex, ref bool dx)
        {
            ++ix;
            if (ix < x.Length)
            {
                cx = x[ix];
                dx = char.IsDigit(cx);
            }
            else
            {
                ex = true;
            }
        }

        private static int CompareAlphaChunk(string x, string y, ref int ix, ref int iy)
        {
            var tmp = 0;
            var cx = x[ix];
            var cy = y[iy];
            var dx = char.IsDigit(cx);
            var dy = char.IsDigit(cy);
            var ex = false;
            var ey = false;

            while (!ex && !ey && !dx && !dy && tmp == 0)
            {
                tmp = cx - cy;

                Advance(x, ref ix, ref cx, ref ex, ref dx);
                Advance(y, ref iy, ref cy, ref ey, ref dy);
            }

            if (tmp == 0)
            {
                if (ex || dx)
                {
                    if (!(ey || dy))
                    {
                        tmp = -1;
                    }
                }
                else if (ey || dy)
                {
                    tmp = 1;
                }
            }

            while (!ex && !dx)
            {
                Advance(x, ref ix, ref cx, ref ex, ref dx);
            }

            while (!ey && !dy)
            {
                Advance(y, ref iy, ref cy, ref ey, ref dy);
            }

            return tmp;
        }

        private static Tuple<int, int> CompareNumericChunk(string x, string y, ref int ix, ref int iy)
        {
            var tmp = 0;
            var cx = x[ix];
            var cy = y[iy];
            var dx = char.IsDigit(cx);
            var dy = char.IsDigit(cy);
            var ex = false;
            var ey = false;
            var nx = 0;
            var ny = 0;
            var oix = ix;
            var oiy = iy;

            while (!ex && dx && cx == '0')
            {
                Advance(x, ref ix, ref cx, ref ex, ref dx);
            }
            while (!ey && dy && cy == '0')
            {
                Advance(y, ref iy, ref cy, ref ey, ref dy);
            }

            while (!ex && !ey && dx && dy && tmp == 0)
            {
                tmp = cx - cy;

                Advance(x, ref ix, ref cx, ref ex, ref dx);
                Advance(y, ref iy, ref cy, ref ey, ref dy);

                ++nx;
                ++ny;
            }

            while (!ex && dx)
            {
                Advance(x, ref ix, ref cx, ref ex, ref dx);
                ++nx;
            }

            while (!ey && dy)
            {
                Advance(y, ref iy, ref cy, ref ey, ref dy);
                ++ny;
            }

            if (nx != ny)
                return Tuple.Create(nx - ny, 0);

            if (tmp == 0)
            {
                // Count of leading zeroes matters with high priority
                return Tuple.Create((iy - oiy) - (ix - oix), 0);

                // Count of leading zeroes matters with low priority
                //return Tuple.Create(0, (iy - oiy) - (ix - oix));
            }

            return Tuple.Create(tmp, 0);
        }

        public int Compare(string x, string y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            if (y == null)
                return 1;

            var xi = 0;
            var yi = 0;
            var tmp = 0;

            while (xi < x.Length && yi < y.Length)
            {
                var xd = char.IsDigit(x[xi]);
                var yd = char.IsDigit(y[yi]);

                if (xd && yd)
                {
                    var c = CompareNumericChunk(x, y, ref xi, ref yi);
                    if (c.Item1 != 0)
                        return c.Item1;

                    if (c.Item2 != 0 && tmp == 0)
                        tmp = c.Item2;
                }
                else if (!xd && !yd)
                {
                    var c = CompareAlphaChunk(x, y, ref xi, ref yi);
                    if (c != 0)
                        return c;
                }
                else
                {
                    return x[xi] - y[yi];
                }
            }

            if (xi < x.Length)
                return 1;

            if (yi < y.Length)
                return -1;

            return tmp;
        }
    }
}
