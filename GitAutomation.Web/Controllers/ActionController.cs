using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.State;
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

        private IActionResult Post(IStandardAction body)
        {
            // TODO - authentication
            dispatcher.Dispatch(body, AnonymousUserAgent.Instance, "Via the UI");
            return Ok();
        }

        [HttpPost]
        public IActionResult CreateReserve([FromBody] CreateReserveAction body)
        {
            return Post(body);
        }
    }
}
