using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.Swagger.Model;
using Swashbuckle.SwaggerGen.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Swagger
{
    public class OperationIdFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var controllerAction = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (controllerAction != null)
            {
                operation.OperationId = $"{controllerAction.ControllerName}.{Clean(controllerAction.ActionName)}";
            }
        }

        private string Clean(string actionName) =>
            actionName.EndsWith("Async")
                ? actionName.Substring(0, actionName.Length - 5)
                : actionName;
    }
}
