using GitAutomation.DomainModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.Controllers
{
    [Route("api/[controller]")]
    public class ActionController : Controller
    {
        private readonly IDispatcher dispatcher;

        public ActionController(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        [HttpPost]
        public IActionResult Post([FromBody] StandardAction body)
        {
            // TODO - authentication
            dispatcher.Dispatch(body, AnonymousUserAgent.Instance);
            return Ok();
        }

    }
}
