using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    static class ImmutableExtensions
    {

        public static ImmutableSortedDictionary<T, U> UpdateItem<T, U>(this ImmutableSortedDictionary<T, U> original, T key, Func<U, U> map)
        {
#pragma warning disable CS8604 // Generics can't handle null reference argument.
            return original.SetItem(key, map(original.TryGetValue(key, out var value) ? value : default));
#pragma warning restore CS8604 // Generics can't handle null reference argument.
        }
    }
}
