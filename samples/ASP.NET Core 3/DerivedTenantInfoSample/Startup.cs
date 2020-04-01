using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Core;
using Finbuckle.MultiTenant.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DerivedTenantInfoSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddMultiTenant().
                WithStore<ConfigurationStore<DerivedTenantInfo>>(ServiceLifetime.Singleton).
                WithBasePathStrategy().
                WithPerTenantOptions<CustomOptions>((options, tenantInfo) =>
                {
                    var derivedTenantInfo = (DerivedTenantInfo)tenantInfo;
                    options.Value1 = derivedTenantInfo.CustomOptions.Value1;
                    options.Value2 = derivedTenantInfo.CustomOptions.Value2;
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseMultiTenant();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("defaultt", "{first_segment=}/{controller=Home}/{action=Index}");
            });
        }
    }
}
