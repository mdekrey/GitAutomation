using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation
{
    static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<U> AsAsyncEnumerable<U>(this IEnumerable<U> target)
        {
            foreach (var e in target)
            {
                await Task.Yield();
                yield return e;
            }
        }

    }
}
