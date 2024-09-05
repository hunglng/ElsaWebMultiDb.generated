using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using ElsaWebMultiDb;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public interface ITenantServiceProvider
{
    RequestDelegate GetPipeline(string hostName, HttpContext context);
    //IServiceProvider GetServiceProvider(TenantInfo tenantInfo);
    IServiceProvider GetServiceProvider(string hostName);

    TenantContext GetTenantContext(string hostName);
}

public class TenantContext
{
    public TenantInfo TenantInfo { get; set; }
    public IServiceProvider? ServiceProvider { get; set; }
    public RequestDelegate? Pipeline { get; set; }
}

public class TenantServiceProvider : ITenantServiceProvider
{
    private readonly Dictionary<string, TenantContext> _tenantContexts = new();
    private readonly List<TenantInfo> _tenantInfos = [
        new TenantInfo() {
            TenantId = "1",
            HostName = "https://localhost:6001",
            ConnectionString = "Data Source=Application1.db;",
        },
        new TenantInfo() {
            TenantId = "2",
            HostName = "https://localhost:6002",
            ConnectionString = "Data Source=Application2.db;",
        }
    ];
    private ICollection<EndpointDataSource> _cacheEnpointDataSources;

    //approach 1
    private bool _routeRegisted = false;

    private readonly IServiceCollection _rootServiceCollection;

    public TenantServiceProvider(IServiceCollection serviceCollection)
    {
        _rootServiceCollection = serviceCollection;
        InitializreSerivces();
    }

    private void InitializreSerivces()
    {
        foreach (var tenantInfo in _tenantInfos)
        {
            if (!_tenantContexts.ContainsKey(tenantInfo.TenantId))
            {
                var tenantContext = new TenantContext();
                var tenantServices = new ServiceCollection();

                foreach (var service in _rootServiceCollection)
                    tenantServices.Add(service);

                //// Add tenant-specific services
                //tenantServices.AddDbContext<ApplicationDbContext>(options =>
                //    options.UseSqlServer(tenantInfo.ConnectionString));

                //tenantServices.AddAuthentication(options =>
                //{
                //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                //}).AddJwtBearer(options =>
                //{

                //});

                tenantServices.AddHttpContextAccessor();
                tenantServices.AddElsa(elsa =>
                {
                    elsa.UseWorkflowManagement(management =>
                    {
                        management.UseEntityFrameworkCore(config =>
                            config.UseSqlite(tenantInfo.ConnectionString));
                    });

                    elsa.UseWorkflowRuntime(runtime =>
                        runtime.UseEntityFrameworkCore(config =>
                            config.UseSqlite(tenantInfo.ConnectionString)));

                    elsa.UseIdentity(identity =>
                    {
                        identity.TokenOptions = options => options.SigningKey = "sufficiently-large-secret-signing-key"; // This key needs to be at least 256 bits long.
                        identity.UseAdminUserProvider();
                    });

                    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());
                    elsa.UseWorkflowsApi(api =>
                    {
                    });
                    elsa.UseWorkflows(); //need to check what is this?
                    elsa.UseRealTimeWorkflows();
                    elsa.UseCSharp();
                    elsa.UseHttp();
                    elsa.UseScheduling();
                    elsa.AddActivitiesFrom<Program>();
                    elsa.AddWorkflowsFrom<Program>();
                });

                var tenantServiceProvider = tenantServices.BuildServiceProvider();
                tenantContext.ServiceProvider = tenantServiceProvider;
                tenantContext.TenantInfo = tenantInfo;
                _tenantContexts.Add(tenantInfo.TenantId, tenantContext);

                var hostedServices = tenantServiceProvider.GetServices<IHostedService>();
                foreach (var service in hostedServices)
                {
                    service.StartAsync(CancellationToken.None);
                }
            }
        }
    }

    //public IServiceProvider GetServiceProvider(TenantInfo tenantInfo)
    //{
    //    return _serviceProviders[tenantInfo.TenantId];
    //}

    public IServiceProvider GetServiceProvider(string hostName)
    {
        var tenantInfo = _tenantInfos.FirstOrDefault(x => x.HostName == hostName);
        if (tenantInfo == null) throw new Exception("Invalid Tenant");
        //return _serviceProviders[tenantInfo.TenantId];

        _tenantContexts.TryGetValue(tenantInfo.TenantId, out var context);
        if (context == null) throw new Exception("Invalid Tenant");
        if (context.ServiceProvider == null) return new ServiceCollection().BuildServiceProvider();
        return context.ServiceProvider;
    }

    public RequestDelegate GetPipeline(string hostName, HttpContext context)
    {
        var tenantInfo = _tenantInfos.FirstOrDefault(x => x.HostName == hostName);
        if (tenantInfo == null) throw new Exception("Invalid Tenant");
        _tenantContexts.TryGetValue(tenantInfo.TenantId, out var tenantContext);
        if (tenantContext == null) throw new Exception("Invalid Tenant");
        if (tenantContext.Pipeline == null)
        {
            //Create pipeline
            var provider = tenantContext.ServiceProvider;

            context.Features.Set<IServiceProvidersFeature>(
                new RequestServicesFeature(context, provider.GetRequiredService<IServiceScopeFactory>()));

            var features = provider.GetService<IServer>()?.Features;
            var builder = new ApplicationBuilder(provider, features ?? new FeatureCollection());
            builder.UseCors();
            builder.UseRouting();
            builder.UseAuthentication();
            builder.UseAuthorization();
            if (!builder.Properties.TryGetValue("__EndpointRouteBuilder", out var obj) ||
                    obj is not IEndpointRouteBuilder routes)
            {
                throw new InvalidOperationException("Failed to retrieve the current endpoint route builder.");
            }
            routes.MapWorkflowsApiCustomized($"{tenantInfo.TenantId}/elsa/api");
            //routes.MapWorkflowsApi($"{tenantInfo.TenantId}/elsa/api");
            //routes.MapWorkflowsApi();

            // This approach do not work because in MapEndpoint of FastEndpoint
            // they will call ServiceProviderServiceExtensions.GetRequiredService<IOptions<AuthorizationOptions>>(app.ServiceProvider).Value;
            // will lead to cannot find policy in correct tenant
            /*
            if (!_routeRegisted)
            {
                routes.MapWorkflowsApi();
                builder.UseWorkflowsSignalRHubs();
                _cacheEnpointDataSources = routes.DataSources;
                _routeRegisted = true;
            }
            else
            {
                routes.DataSources.AddRange(_cacheEnpointDataSources);
            }*/

            builder.UseWorkflows();
            builder.UseWorkflowsSignalRHubs();

            builder.UseEndpoints(routes =>
            {
            });

            tenantContext.Pipeline = builder.Build();
        }
        else
        {
            var provider = tenantContext.ServiceProvider;

            context.Features.Set<IServiceProvidersFeature>(
                new RequestServicesFeature(context, provider.GetRequiredService<IServiceScopeFactory>()));

        }
        var contextAccessor = tenantContext.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        if (contextAccessor != null)
        {
            contextAccessor.HttpContext = context;
        }

        return tenantContext.Pipeline;
    }

    public TenantContext GetTenantContext(string hostName)
    {
        var tenantInfo = _tenantInfos.FirstOrDefault(x => x.HostName == hostName);
        if (tenantInfo == null) throw new Exception("Invalid Tenant");
        _tenantContexts.TryGetValue(tenantInfo.TenantId, out var context);
        if (context == null) throw new Exception("Invalid Tenant");
        return context;
    }
}

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddTenantServiceProvider(this IServiceCollection services)
    {
        var tenantProvider = new TenantServiceProvider(services);

        services.AddSingleton<ITenantServiceProvider>(tenantProvider);

        return services;
    }
}