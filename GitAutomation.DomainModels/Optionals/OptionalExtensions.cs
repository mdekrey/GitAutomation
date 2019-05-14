using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Optionals
{
    public static class OptionalExtensions
    {
        public static Optional<T> IfStringMatch<T>(this T original, object target)
        {
            if (original?.ToString() == target?.ToString())
            {
                return Optional<T>.Of(original);
            }
            return Optional<T>.Empty;
        }
    }
}
