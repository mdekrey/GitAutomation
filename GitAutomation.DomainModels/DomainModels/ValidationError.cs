using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class ValidationError
    {
        public ValidationError(string errorCode)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; }
        public Dictionary<string, string> Arguments { get; } = new Dictionary<string, string>();
    }
}
