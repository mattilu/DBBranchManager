using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

    public class NaturalSortComparer2 : IComparer<string>, IDisposable
    {
        private bool isAscending;

        public NaturalSortComparer2(bool inAscendingOrder = true)
        {
            this.isAscending = inAscendingOrder;
        }

        #region IComparer<string> Members

        public int Compare(string x, string y)
        {
            throw new NotImplementedException();
        }

        #endregion IComparer<string> Members

        #region IComparer<string> Members

        int IComparer<string>.Compare(string x, string y)
        {
            if (x == y)
                return 0;

            string[] x1, y1;

            if (!table.TryGetValue(x, out x1))
            {
                x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
                table.Add(x, x1);
            }

            if (!table.TryGetValue(y, out y1))
            {
                y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)");
                table.Add(y, y1);
            }

            int returnVal;

            for (int i = 0; i < x1.Length && i < y1.Length; i++)
            {
                if (x1[i] != y1[i])
                {
                    returnVal = PartCompare(x1[i], y1[i]);
                    return isAscending ? returnVal : -returnVal;
                }
            }

            if (y1.Length > x1.Length)
            {
                returnVal = 1;
            }
            else if (x1.Length > y1.Length)
            {
                returnVal = -1;
            }
            else
            {
                returnVal = 0;
            }

            return isAscending ? returnVal : -returnVal;
        }

        private static int PartCompare(string left, string right)
        {
            int x, y;
            if (!int.TryParse(left, out x))
                return left.CompareTo(right);

            if (!int.TryParse(right, out y))
                return left.CompareTo(right);

            return x.CompareTo(y);
        }

        #endregion IComparer<string> Members

        private Dictionary<string, string[]> table = new Dictionary<string, string[]>();

        public void Dispose()
        {
            table.Clear();
            table = null;
        }
    }

    public class AlphanumComparator : IComparer<string>
    {
        private enum ChunkType { Alphanumeric, Numeric };

        private bool InChunk(char ch, char otherCh)
        {
            ChunkType type = ChunkType.Alphanumeric;

            if (char.IsDigit(otherCh))
            {
                type = ChunkType.Numeric;
            }

            if ((type == ChunkType.Alphanumeric && char.IsDigit(ch))
                || (type == ChunkType.Numeric && !char.IsDigit(ch)))
            {
                return false;
            }

            return true;
        }

        public int Compare(string x, string y)
        {
            String s1 = x as string;
            String s2 = y as string;
            if (s1 == null || s2 == null)
            {
                return 0;
            }

            int thisMarker = 0, thisNumericChunk = 0;
            int thatMarker = 0, thatNumericChunk = 0;

            while ((thisMarker < s1.Length) || (thatMarker < s2.Length))
            {
                if (thisMarker >= s1.Length)
                {
                    return -1;
                }
                else if (thatMarker >= s2.Length)
                {
                    return 1;
                }
                char thisCh = s1[thisMarker];
                char thatCh = s2[thatMarker];

                StringBuilder thisChunk = new StringBuilder();
                StringBuilder thatChunk = new StringBuilder();

                while ((thisMarker < s1.Length) && (thisChunk.Length == 0 || InChunk(thisCh, thisChunk[0])))
                {
                    thisChunk.Append(thisCh);
                    thisMarker++;

                    if (thisMarker < s1.Length)
                    {
                        thisCh = s1[thisMarker];
                    }
                }

                while ((thatMarker < s2.Length) && (thatChunk.Length == 0 || InChunk(thatCh, thatChunk[0])))
                {
                    thatChunk.Append(thatCh);
                    thatMarker++;

                    if (thatMarker < s2.Length)
                    {
                        thatCh = s2[thatMarker];
                    }
                }

                int result = 0;
                // If both chunks contain numeric characters, sort them numerically
                if (char.IsDigit(thisChunk[0]) && char.IsDigit(thatChunk[0]))
                {
                    thisNumericChunk = Convert.ToInt32(thisChunk.ToString());
                    thatNumericChunk = Convert.ToInt32(thatChunk.ToString());

                    if (thisNumericChunk < thatNumericChunk)
                    {
                        result = -1;
                    }

                    if (thisNumericChunk > thatNumericChunk)
                    {
                        result = 1;
                    }
                }
                else
                {
                    result = thisChunk.ToString().CompareTo(thatChunk.ToString());
                }

                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }
    }
}