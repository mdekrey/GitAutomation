using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Management
{
    [Route("")]
    [Route("management")]
    public class ManagementController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/management/Index.cshtml");
        }

        [HttpGet("gitdir")]
        public IActionResult GitDirectory()
        {
            return Ok(System.IO.Directory.GetFiles("/working"));
        }
    }
}
