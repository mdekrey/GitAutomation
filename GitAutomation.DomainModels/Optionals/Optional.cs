using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Optionals
{
    public class Optional<T>
    {
        private readonly bool hasValue;
        private readonly T value;

        public static readonly Optional<T> Empty = new Optional<T>();

        private Optional()
        {
            hasValue = false;
#nullable disable
            value = default;
#nullable restore
        }

        public Optional(T value)
        {
            this.value = value;
            this.hasValue = true;
        }

        public static Optional<T> Of(T target) =>
            new Optional<T>(target);

        public T OrElse(T orElse)
        {
            return hasValue ? value : orElse;
        }

        public T OrElse(Func<T> func)
        {
            return hasValue ? value : func();
        }

        public void IfPresent(Action<T> action)
        {
            if (hasValue)
            {
                action(value);
            }
        }

        public Optional<U> Map<U>(Func<T, U> map) =>
            hasValue ? Optional<U>.Of(map(value)) : Optional<U>.Empty;
    }
}
