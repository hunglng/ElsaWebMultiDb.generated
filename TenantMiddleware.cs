using Elsa.Extensions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace ElsaWebMultiDb
{
    /*
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITenantServiceProvider _tenantServiceProvider;
        private readonly IApplicationBuilder _rootApp;

        public TenantMiddleware(RequestDelegate next,
            ITenantServiceProvider tenantServiceProvider,
            IApplicationBuilder app)
        {
            _next = next;
            _tenantServiceProvider = tenantServiceProvider;
            _rootApp = app;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //// Extract tenant info from the request (e.g., from headers, query string, or path)
            //var tenantId = context.Request.Headers["TenantId"].ToString();
            //var tenantInfo = GetTenantInfo(tenantId);

            var hostname = context.Request.Headers.Origin;
            var a = new List<string>() { "https://localhost:6002", "https://localhost:6001" };

            if (a.Contains(hostname))
            {
                var tenantServiceProvider = _tenantServiceProvider.GetServiceProvider(hostname);

                if (tenantServiceProvider != null)
                {
                    await NotHandleAppUse(context, tenantServiceProvider);
                }
                else
                {
                    await _next(context);
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task NotHandleAppUse(HttpContext context, IServiceProvider tenantServiceProvider)
        {
            IServiceProvidersFeature existingFeature = null!;
            try
            {
                existingFeature = context.Features.Get<IServiceProvidersFeature>()!;
                context.Features.Set<IServiceProvidersFeature>(
                    new RequestServicesFeature(context, tenantServiceProvider.GetRequiredService<IServiceScopeFactory>()));
                await _next.Invoke(context);
            }
            finally
            {
                // Restore the original feature if it was replaced (in case it is used before the response ends)
                context.Features.Set(existingFeature);
            }
        }

        private static async ValueTask ConfigurePipelineAsync(ApplicationBuilder builder)
        {
            // 'IStartup' instances are ordered by module dependencies with a 'ConfigureOrder' of 0 by default.
            // 'OrderBy' performs a stable sort, so the order is preserved among equal 'ConfigureOrder' values.
            //var startups = builder.ApplicationServices.GetServices<IStartup>().OrderBy(s => s.ConfigureOrder);

            // Should be done first.
            builder.UseRouting();

            // Try to retrieve the current 'IEndpointRouteBuilder'.
            if (!builder.Properties.TryGetValue("__EndpointRouteBuilder", out var obj) ||
                obj is not IEndpointRouteBuilder routes)
            {
                throw new InvalidOperationException("Failed to retrieve the current endpoint route builder.");
            }

            // Routes can be then configured outside 'UseEndpoints()'.
            //var services = ShellScope.Services;
            //foreach (var startup in startups)
            //{
            //    if (startup is IAsyncStartup asyncStartup)
            //    {
            //        await asyncStartup.ConfigureAsync(builder, routes, services);
            //    }

            //    startup.Configure(builder, routes, services);
            //}

            // Knowing that routes are already configured.
            builder.UseEndpoints(routes => { });

            //builder.UseAuthentication();
            //builder.UseAuthorization();
            //builder.UseWorkflowsApi();
            //builder.UseWorkflows();
            //builder.UseWorkflowsSignalRHubs();
        }
    }
    */

    public class TenantMiddleware
    {
        private RequestDelegate _next;
        private ITenantServiceProvider _tenantServiceProvider;

        public TenantMiddleware(RequestDelegate next, ITenantServiceProvider tenantServiceProvider)
        {
            _next = next;
            _tenantServiceProvider = tenantServiceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //// Extract tenant info from the request (e.g., from headers, query string, or path)
            //var tenantId = context.Request.Headers["TenantId"].ToString();
            //var tenantInfo = GetTenantInfo(tenantId);

            var hostname = context.Request.Headers.Origin;
            var a = new List<string>() { "https://localhost:6002", "https://localhost:6001" };

            if (a.Contains(hostname))
            {
                IServiceProvidersFeature existingFeature = null!;
                try
                {
                    existingFeature = context.Features.Get<IServiceProvidersFeature>()!;


                    var tenantContext = _tenantServiceProvider.GetTenantContext(hostname);
                    if (null != tenantContext.ServiceProvider)
                    {
                        //rewrite url
                        //var path = context.Request.Path;

                        var prefix = tenantContext.TenantInfo.TenantId;
                        //context.Request.PathBase = $"/{prefix}";

                        context.Request.Path = $"/{prefix}{context.Request.Path}";

                        context.Features.Set<IServiceProvidersFeature>(
                            new RequestServicesFeature(context, tenantContext.ServiceProvider.GetRequiredService<IServiceScopeFactory>()));
                    }
                    var pipeline = _tenantServiceProvider.GetPipeline(hostname, context);

                    #region moved to GetPipeline logic
                    //if (pipeline == null)
                    //{
                    //    var provider = _tenantServiceProvider.GetServiceProvider(hostname);
                    //    context.Features.Set<IServiceProvidersFeature>(
                    //        new RequestServicesFeature(context, provider.GetRequiredService<IServiceScopeFactory>()));

                    //    //await _next.Invoke(context);

                    //    var features = provider.GetService<IServer>()?.Features;
                    //    var builder = new ApplicationBuilder(provider, features ?? new FeatureCollection());
                    //    builder.UseCors();
                    //    builder.UseRouting();
                    //    builder.UseAuthentication();
                    //    builder.UseAuthorization();
                    //    //var b = builder.Properties["__EndpointRouteBuilder"];
                    //    if (!builder.Properties.TryGetValue("__EndpointRouteBuilder", out var obj) ||
                    //                obj is not IEndpointRouteBuilder routes)
                    //    {
                    //        throw new InvalidOperationException("Failed to retrieve the current endpoint route builder.");
                    //    }
                    //    //(obj as IApplicationBuilder).UseWorkflowsApi();
                    //    routes.MapWorkflowsApi();
                    //    builder.UseWorkflows();
                    //    builder.UseWorkflowsSignalRHubs();

                    //    builder.UseEndpoints(routes => { });

                    //    pipeline = builder.Build();
                    //} 
                    #endregion

                    await pipeline.Invoke(context);
                }
                finally
                {
                    // Restore the original feature if it was replaced (in case it is used before the response ends)
                    context.Features.Set(existingFeature);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }

    public static class ApplicationBuilderExtension
    {
        public static IApplicationBuilder UseTenantBuilder(this IApplicationBuilder app)
        {
            //app.UseCors();
            //app.UseRouting();
            app.UseMiddleware<TenantMiddleware>();


            return app;
        }
    }
}
