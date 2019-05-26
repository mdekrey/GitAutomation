using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.Scripts
{
    public class PowerShellLine
    {
        public string Comment { get; set; }
        public StandardAction? Action { get; set; }
    }

}
