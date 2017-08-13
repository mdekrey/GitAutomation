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
    [Route("api/[controller]")]
    public class ManagementController : Controller
    {
        private readonly IRepositoryState repositoryState;

        public ManagementController(IRepositoryState repositoryState)
        {
            this.repositoryState = repositoryState;
        }
        
        [HttpGet("remote-branches")]
        public async Task<IActionResult> RemoteBranches()
        {
            return Ok(await repositoryState.RemoteBranches().FirstAsync());
        }
    }
}
