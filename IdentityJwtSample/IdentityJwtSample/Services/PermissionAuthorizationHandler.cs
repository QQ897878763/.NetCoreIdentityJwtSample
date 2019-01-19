using IdentityJwtSample.Filters;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityJwtSample.Services
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>, IDependency
    {

        private List<string> permissions = new List<string>() {
            "Create",
            "Delete",
            "Select"
        };

        public PermissionAuthorizationHandler()
        {

        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionAuthorizationRequirement requirement)
        {
            if (context.User != null)
            {                 
                //检查是否有权限
                var matchedItems = permissions.Intersect(requirement.Permissions);
                if (matchedItems?.Count() > 0)
                    context.Succeed(requirement);             
            }
            return Task.CompletedTask;
        }
    }
}
