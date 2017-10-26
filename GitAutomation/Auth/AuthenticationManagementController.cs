using GitAutomation.Work;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Auth
{
    [Route("api/[controller]")]
    public class AuthenticationManagementController : Controller
    {
        private readonly IUnitOfWorkFactory unitOfWorkFactory;
        private readonly IManageUserPermissions permissions;

        public AuthenticationManagementController(IUnitOfWorkFactory unitOfWorkFactory, IManageUserPermissions permissions)
        {
            this.unitOfWorkFactory = unitOfWorkFactory;
            this.permissions = permissions;
        }
        
        [Authorize(Auth.PolicyNames.Administrate)]
        [HttpPut("user/{*userName}")]
        public async Task<IActionResult> UpdateUser(string userName, [FromBody] UpdateUserRequestBody requestBody)
        {
            using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
            {
                foreach (var addedRole in requestBody.AddRoles)
                {
                    permissions.AddUserRole(userName, addedRole, unitOfWork);
                }
                foreach (var removedRole in requestBody.RemoveRoles)
                {
                    permissions.RemoveUserRole(userName, removedRole, unitOfWork);
                }

                await unitOfWork.CommitAsync();
            }

            return Ok(await permissions.GetUsersAndRoles());
        }

        public class UpdateUserRequestBody
        {
            public string[] AddRoles { get; set; }
            public string[] RemoveRoles { get; set; }
        }
    }
}
