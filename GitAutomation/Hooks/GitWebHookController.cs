using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Hooks
{
    [Route("api/[controller]")]
    public class GitWebHookController : Controller
    {
        [HttpPost]
        public void Post()
        {
            // TODO - queue pull and other processing
        }
    }
}
