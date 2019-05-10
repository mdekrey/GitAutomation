using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitAutomation
{
    static class StringHelpers
    {
        public static string[] ToComparableText<T>(IEnumerable<T> target, Func<T, String> toString) =>
           (from t in target
            let result = toString(t)
            orderby result
            select result).ToArray();


        public static string FixLineEndings(this string text) =>
            text.Replace("\r", "");
    }
}
