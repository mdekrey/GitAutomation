using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Mvc
{
    public class MvcExtensionOptions
    {
        /// <summary>
        /// An array of <see cref="IMvcExtension"/> type names
        /// </summary>
        public string[] Types { get; set; }
    }
}
