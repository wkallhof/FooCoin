using System;
using System.Collections.Generic;

namespace FooCoin.Core.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<TResult> SelectTwo<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return SelectTwoImpl(source, selector);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        private static IEnumerable<TResult> SelectTwoImpl<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> selector)
        {
            using (var iterator = source.GetEnumerator())
            {
                var item2 = default(TSource);
                var i = 0;
                while (iterator.MoveNext())
                {
                    var item1 = item2;
                    item2 = iterator.Current;
                    i++;

                    if (i >= 2)
                    {
                        yield return selector(item1, item2);
                    }
                }
            }
        }
    }
}