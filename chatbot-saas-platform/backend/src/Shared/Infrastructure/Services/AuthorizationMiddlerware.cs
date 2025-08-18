using Microsoft.AspNetCore.Http;
using Shared.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Services
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService, ITenantService tenantService)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null)
            {
                // Skip auth check for endpoints marked [AllowAnonymous]
                await _next(context);
                return;
            }

            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            // Check user role
            var userRole = currentUserService.Role;
            if (string.IsNullOrEmpty(userRole))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden: No role assigned");
                return;
            }
            Guid? tenantId;
            // If not SuperAdmin or Admin, validate tenant
            if (!userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                !userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                 tenantId = currentUserService.TenantId;
                if (!tenantId.HasValue /*|| !await tenantService.TenantExistsAsync(tenantId.Value)*/)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Forbidden: Invalid tenant");
                    return;
                }
            }
            // tenantId = currentUserService.TenantId;
            //if (!tenantId.HasValue || !await tenantService.TenantExistsAsync(tenantId.Value))
            //{
            //    context.Response.StatusCode = StatusCodes.Status403Forbidden;
            //    await context.Response.WriteAsync("Forbidden: Invalid tenant");
            //    return;
            //}

            await _next(context);
        }

    }
}



