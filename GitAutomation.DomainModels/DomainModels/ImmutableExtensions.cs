﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    static class ImmutableExtensions
    {

        public static ImmutableSortedDictionary<T, U> UpdateItem<T, U>(this ImmutableSortedDictionary<T, U> original, T key, Func<U, U> map)
        {
#nullable disable
            return original.SetItem(key, map(original.TryGetValue(key, out var value) ? value : default));
#nullable restore
        }
    }
}
