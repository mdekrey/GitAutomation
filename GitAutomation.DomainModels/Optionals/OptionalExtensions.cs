using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Optionals
{
    public static class OptionalExtensions
    {
        public static Optional<DateTimeOffset> IfApproximateMatch(this DateTimeOffset original, DateTimeOffset target)
        {
            try
            {
                var diff = original - target;
                if (Math.Abs(diff.TotalSeconds) < 1)
                {
                    return Optional<DateTimeOffset>.Of(original);
                }
                return Optional<DateTimeOffset>.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                
                return Optional<DateTimeOffset>.Empty;
            }
        }
    }
}
