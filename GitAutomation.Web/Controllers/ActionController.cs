﻿using GitAutomation.DomainModels;
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

        [HttpPost]
        public IActionResult Post([FromBody] IStandardAction body)
        {
            // TODO - authentication
            dispatcher.Dispatch(new StateUpdateEvent<IStandardAction>(body, AnonymousUserAgent.Instance, "Via the UI"));
            return Ok();
        }

    }
}
