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
            // Check if user is authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            // Check if user belongs to a valid tenant
            var tenantId = currentUserService.TenantId;
            if (!tenantId.HasValue || !await tenantService.TenantExistsAsync(tenantId.Value))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden: Invalid tenant");
                return;
            }

            await _next(context);
        }
        }
    }



