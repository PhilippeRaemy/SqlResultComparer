using System;
using System.Collections.Generic;
using System.Linq;

namespace ResultsComparer
{
    public class Result : List<IEnumerable<object>>, IEquatable<Result>
    {


        public Result() { }

        private Result(IEnumerable<IEnumerable<object>> li)
        {
            AddRange(li);
        }

        public string[] ColumnNames;

        public bool Equals(Result other)
        {
            return this.SequenceEqual(other, new EnumerableEqualityComparer { NullIsEmptyString = true, NullIsZero = true, NullIsDbNull = true });
        }


        private Func<IEnumerable<object>, string> OrderByLambda(int idx)
        {
            return r =>
                {
                    var elm = r.ElementAt(idx);
                    return elm == null || elm is DBNull
                               ? string.Empty
                               : EnumerableEqualityComparer.IsNumeric(elm)
                                     ? string.Format("{0:0000000000000000.0000000000000000}", elm)
                                     : elm.ToString();
                };
        }

        public Result Diff(Result other, string[] ignoredColumns = null, int alignOnFirst = 0)
        {
            var comparer = new EnumerableEqualityComparer { NullIsEmptyString = true, NullIsZero = true, NullIsDbNull = true };
            var colcount = ColumnNames.Count() > other.ColumnNames.Count()
                               ? ColumnNames.Count()
                               : other.ColumnNames.Count();
            var ignoredOrdinals = ignoredColumns == null
                                      ? new int[] {}
                                      : ColumnNames.Select((c, i) => new {c, i})
                                                   .Where(ci => ignoredColumns.Contains(ci.c))
                                                   .Select(ci => ci.i)
                                                   .ToArray();

            var orderedThis = this.OrderBy(OrderByLambda(0));
            for (var i = 1; i < this.ColumnNames.Count(); i++) if (!ignoredOrdinals.Contains(i))
                {
                    var j = i;
                    orderedThis = orderedThis.ThenBy(OrderByLambda(j));
                }
            var orderedOther = other.OrderBy(OrderByLambda(0));
            for (var i = 1; i < other.ColumnNames.Count(); i++) if (!ignoredOrdinals.Contains(i))
                {
                    var j = i;
                    orderedOther = orderedOther.ThenBy(OrderByLambda(j));
                }
            var res = new Result(orderedThis.OuterZip(orderedOther
                , r1 => string.Join("|", r1.Take(alignOnFirst))
                , r2 => string.Join("|", r2.Take(alignOnFirst))
                , (r1, r2) =>
                {
                    if (r1 == null) return new[] { new object[] { ">" }.Concat(r2).Concat(Enumerable.Repeat((string)null, colcount - r2.Count())) };
                    if (r2 == null) return new[] { new object[] { "<" }.Concat(r1).Concat(Enumerable.Repeat((string)null, colcount - r1.Count())) };
                    if (!comparer.Equals(r1, r2, ignoredOrdinals))
                    {
                        var diffs = comparer.DiffFlags(r1, r2, ignoredOrdinals).ToArray();
                        return new[]
                            {
                                new object[] {"--"}.Concat(Enumerable.Repeat("--", colcount)),
                                new object[] {"<"}.Concat(r1).Concat(Enumerable.Repeat((string)null, colcount-r1.Count())),
                                new object[] {">"}.Concat(r2).Concat(Enumerable.Repeat((string)null, colcount-r2.Count())),
                                new object[] {"<>"}.Concat(diffs).Concat(Enumerable.Repeat((string)null, colcount-diffs.Count())),
                            };
                    }
                    return new[] { new object[] { "=" }.Concat(r1) };
                })
                                            .SelectMany(t => t.ToArray()));
            res.ColumnNames = new[] { "Diff" }
                .Concat(
                    ColumnNames.OuterZip(other.ColumnNames, (c1, c2) => c1 == c2 ? c1 : (c1 ?? "-") + "/" + (c2 ?? "-"))
                ).ToArray();
            return res;
        }
        public override string ToString()
        {
            var stringified = this.Select(r => r.Select(v => v == null ? "{Null}" : v.ToString()).ToArray()).ToArray();
            var widths = ColumnNames.Select(n => n.Length).ToArray();
            foreach (var r in stringified)
            {
                for (int i = 0; i < r.Count(); i++)
                {
                    var l = r[i].Length;
                    //if (i >= widths.Length) widths = widths.Concat(new[] {l}).ToArray(); else 
                    if (widths[i] < l) widths[i] = l;
                }
            }
            var format = string.Join(string.Empty, widths.Select((w, i) => string.Format(" {{{1},{0}}} |", w, i)));
            return string.Format(format, ColumnNames) + "\r\n"
                   + string.Join(" ", widths.Select(w => new string('-', w + 2))) + "\r\n"
                   + string.Join("\r\n", stringified.Select(r => string.Format(format, r)));
        }
    }
}