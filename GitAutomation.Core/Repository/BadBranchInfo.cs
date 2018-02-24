using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Repository
{
    public class BadBranchInfo
    {
        public string Commit { get; set; }
        public string ReasonCode { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
