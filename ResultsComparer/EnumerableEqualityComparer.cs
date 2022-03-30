using System;
using System.Collections.Generic;
using System.Linq;

namespace ResultsComparer
{
    public class EnumerableEqualityComparer : IEqualityComparer<IEnumerable<object>>
    {
        public bool NullIsZero = false;
        public bool NullIsEmptyString = false;
        public bool NullIsDbNull = false;
        public int NumericPrecision = 2;
        public static bool IsNumeric(object value)
        {
            return value is sbyte
                   || value is byte
                   || value is short
                   || value is ushort
                   || value is int
                   || value is uint
                   || value is long
                   || value is ulong
                   || value is float
                   || value is double
                   || value is decimal;
        }

        private IEnumerable<bool?> Flags(IEnumerable<object> x, IEnumerable<object> y, int[] ignoredOrdinals = null)
        {
            if (x == null) x = new object[] { };
            if (y == null) y = new object[] { };
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (NullIsDbNull)
            {
                x = x.Select(a => a is DBNull ? null : a);
                y = y.Select(b => b is DBNull ? null : b);
            }
            return x.OuterZip(y, 
                              (a, b, i) => ignoredOrdinals!=null && ignoredOrdinals.Contains(i)
                                  ? (bool?)null
                                  : (a == null && b == null)
                                      || (IsNumeric(a) && IsNumeric(b) && Math.Round(Convert.ToDouble(a) - Convert.ToDouble(b), NumericPrecision) == 0.0)
                                      || (a != null && a.Equals(b))
                                      || (NullIsZero && a==null && IsNumeric(b) && Math.Round(Convert.ToDouble(b), NumericPrecision) == 0.0)
                                      || (NullIsZero && b==null && IsNumeric(a) && Math.Round(Convert.ToDouble(a), NumericPrecision) == 0.0)
                                      || (NullIsEmptyString && a == null && b.ToString() == string.Empty)
                                      || (NullIsEmptyString && b == null && a.ToString() == string.Empty)
                );
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        public bool Equals(IEnumerable<object> x, IEnumerable<object> y)
        {
            return Flags(x, y).All(b => !b.HasValue || b.Value);
        }
        public bool Equals(IEnumerable<object> x, IEnumerable<object> y, int[] ignoredOrdinals)
        {
            return Flags(x, y, ignoredOrdinals).All(b => !b.HasValue || b.Value);
        }

        public int GetHashCode(IEnumerable<object> obj)
        {
            return obj.GetHashCode();
        }

        public IEnumerable<string> DiffFlags(IEnumerable<object> x, IEnumerable<object> y, int[] ignoredOrdinals=null)
        {
            return Flags(x, y, ignoredOrdinals).Select(b => !b.HasValue ? "na" : b.Value ? "=" : "<>");
        }
    }
}