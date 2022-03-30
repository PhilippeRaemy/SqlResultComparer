using System;
using System.Collections.Generic;

namespace ResultsComparer
{
    public static class EnumerableExtentions
    {
        public static IEnumerable<TResult> OuterZip<TResult, T1, T2>(
            this IEnumerable<T1> me, IEnumerable<T2> other,
            Func<T1, T2, int, TResult> resultor)
        {
            var i = 0;
            if (other == null)
            {
                foreach (var r in me)
                    yield return resultor(r, default, i++);
                yield break;
            }

            using (var meEnum = me.GetEnumerator())
            using (var otherEnum = other.GetEnumerator())
                while (true)
                {
                    var iHaveMore = meEnum.MoveNext();
                    var heHasMore = otherEnum.MoveNext();
                    if (!(iHaveMore || heHasMore)) yield break;
                    yield return resultor(iHaveMore ? meEnum.Current : default, heHasMore ? otherEnum.Current : default, i++);
                }
        }

        public static IEnumerable<TResult> OuterZip<TResult, T1, T2>(this IEnumerable<T1> me, IEnumerable<T2> other, Func<T1, T2, TResult> resultor)
            => me.OuterZip(other, (a, b, i) => resultor(a, b));

        public static IEnumerable<TResult> OuterZip<TResult, T1, T2, TAlign>(
            this IEnumerable<T1> me, IEnumerable<T2> other,
            Func<T1, int, TAlign> meAligner,
            Func<T2, int, TAlign> otherAligner,
            Func<T1, T2, int, TResult> resultor)
        {
            var i = 0;
            if (other == null)
            {
                foreach (var r in me)
                    yield return resultor(r, default, i++);
                yield break;
            }

            var meAlign = default(TAlign);
            var prevMeAlign = default(TAlign);
            var otherAlign = default(TAlign);
            var prevOtherAlign = default(TAlign);
            var meProgress = true;
            var otherProgress = true;
            var iHaveMore = false;
            var heHasMore = false;
            using (var meEnum = me.GetEnumerator())
            using (var otherEnum = other.GetEnumerator())
                while (true)
                {
                    if (meProgress)
                    {
                        iHaveMore = meEnum.MoveNext();
                        meAlign = iHaveMore ? meAligner(meEnum.Current, i) : default;
                    }

                    if (otherProgress)
                    {
                        heHasMore = otherEnum.MoveNext();
                        otherAlign = heHasMore ? otherAligner(otherEnum.Current, i) : default;
                    }

                    if (!(iHaveMore || heHasMore)) yield break;
                    if (!iHaveMore || !heHasMore || meAlign.Equals(otherAlign))
                    {
                        meProgress = true;
                        otherProgress = true;
                        yield return
                            resultor(iHaveMore ? meEnum.Current : default,
                                heHasMore ? otherEnum.Current : default, i++);
                    }
                    // now the Aligns differ and both have more...
                    // find out which breaks...
                    else if (meAlign.Equals(prevMeAlign))
                    {
                        otherProgress = false;
                        yield return resultor(meEnum.Current, default, i++);
                    }
                    else if (otherAlign.Equals(prevOtherAlign))
                    {
                        meProgress = false;
                        yield return resultor(default, otherEnum.Current, i++);
                    }
                    else
                    {
                        meProgress = true;
                        otherProgress = true;
                        yield return resultor(meEnum.Current, default, i++);
                        yield return resultor(default, otherEnum.Current, i++);
                    }

                    if (meProgress) prevMeAlign = meAlign;
                    if (otherProgress) prevOtherAlign = otherAlign;
                }
        }

        public static IEnumerable<TResult> OuterZip<TResult, T1, T2, TAlign>(this IEnumerable<T1> me, IEnumerable<T2> other,
            Func<T1, TAlign> meAligner,
            Func<T2, TAlign> otherAligner,
            Func<T1, T2, TResult> resultor)
            => me.OuterZip(other, (a, i) => meAligner(a), (b, i) => otherAligner(b), (a, b, i) => resultor(a, b));
    }
}