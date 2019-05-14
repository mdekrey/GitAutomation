using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation
{
    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<U> AsAsyncEnumerable<U>(this IEnumerable<U> target)
        {
            foreach (var e in target)
            {
                await Task.Yield();
                yield return e;
            }
        }

        public static async IAsyncEnumerable<U> Select<T, U>(this IAsyncEnumerable<T> target, Func<T, U> map)
        {
            await foreach (var e in target)
            {
                yield return map(e);
            }
        }
    }
}
