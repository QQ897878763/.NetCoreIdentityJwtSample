using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityJwtSample.Filters
{

    /// <summary>
    /// 权限授权处理参数
    /// </summary>
    public class PermissionAuthorizationRequirement : IAuthorizationRequirement
    {
        public string[] Permissions { get; }

        public PermissionAuthorizationRequirement(string[] permissions)
        {
            Permissions = permissions;
        }
    }

    /// <summary>
    /// 权限授权过滤器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PermissionFilter : Attribute, IAsyncAuthorizationFilter
    {
        /// <summary>
        /// 传入的权限集合
        /// </summary>
        public string[] Permissions { get; }
        /// <summary>
        /// 过滤器构造函数
        /// </summary>
        /// <param name="permissions"></param>
        public PermissionFilter(params string[] permissions)
        {
            Permissions = permissions;
        }


        /// <summary>
        /// 授权处理
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            var authorizationResult = await authorizationService.AuthorizeAsync(context.HttpContext.User, null,
                new PermissionAuthorizationRequirement(Permissions));
            if (!authorizationResult.Succeeded)
            {
                throw new UnauthorizedAccessException("访问被拒绝!权限未提供或服务令牌过期!");
            }
        }
    }
}
