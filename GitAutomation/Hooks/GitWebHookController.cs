using GitAutomation.BranchSettings;
using GitAutomation.Repository;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Hooks
{
    [Route("api/[controller]")]
    public class GitWebHookController : Controller
    {
        [HttpPost]
        public void Post([FromServices] IRepositoryMediator repository)
        {
            repository.CheckForUpdates();
        }
    }
}
