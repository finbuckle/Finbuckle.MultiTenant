﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Finbuckle.MultiTenant.AspNetCore
{
    internal class MultiTenantAuthenticationService : AuthenticationService
    {
        public MultiTenantAuthenticationService(IAuthenticationSchemeProvider schemes,
                                                IAuthenticationHandlerProvider handlers,
                                                IClaimsTransformation transform,
                                                IOptions<AuthenticationOptions> options) : base(schemes, handlers, transform, options)
        {
        }

        public override async Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            // Add tenant identifier to the properties so on the callback we can use it to set the multitenant context.
            var multiTenantContext = context.GetMultiTenantContext();
            if (multiTenantContext.TenantInfo != null)
            {
                properties = properties ?? new AuthenticationProperties();
                properties.Items.Add("tenantIdentifier", multiTenantContext.TenantInfo.Identifier);
            }

            await base.ChallengeAsync(context, scheme, properties);
        }
    }
}
