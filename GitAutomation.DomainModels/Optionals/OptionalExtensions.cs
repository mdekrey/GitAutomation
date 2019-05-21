using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Optionals
{
    public static class OptionalExtensions
    {
        public static Optional<DateTimeOffset> IfApproximateMatch(this DateTimeOffset original, object target)
        {
            try
            {
                var diff = target is DateTimeOffset dto ? (original - dto) : target is DateTime dt ? (original - dt) : (original - DateTimeOffset.Parse(target.ToString()));
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
