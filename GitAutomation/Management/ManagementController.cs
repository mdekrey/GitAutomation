using GitAutomation.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace GitAutomation.Management
{
    [Route("")]
    [Route("management")]
    public class ManagementController : Controller
    {
        private readonly IRepositoryState repositoryState;

        public ManagementController(IRepositoryState repositoryState)
        {
            this.repositoryState = repositoryState;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/management/Index.cshtml");
        }
        
        [HttpGet("remote-branches")]
        public async Task<IActionResult> RemoteBranches()
        {
            return Ok(await repositoryState.RemoteBranches().FirstAsync());
        }
    }
}
