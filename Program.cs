using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using ElsaWebMultiDb;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddSingleton<TenantInfo>();
//builder.Services.AddSingleton<TenantInfo>();
#region old
//builder.Services.AddElsa(elsa =>
//{
//    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore());

//    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore());

//    elsa.UseIdentity(identity =>
//    {
//        identity.TokenOptions = options => options.SigningKey = "sufficiently-large-secret-signing-key"; // This key needs to be at least 256 bits long.
//        identity.UseAdminUserProvider();
//    });

//    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());
//    elsa.UseWorkflowsApi();
//    elsa.UseRealTimeWorkflows();
//    elsa.UseCSharp();
//    elsa.UseHttp();
//    elsa.UseScheduling();
//    elsa.AddActivitiesFrom<Program>();
//    elsa.AddWorkflowsFrom<Program>();
//});

//builder.Services.AddElsa(elsa =>
//{
//    elsa.UseIdentity(identity =>
//    {
//        identity.TokenOptions = options => options.SigningKey = "sufficiently-large-secret-signing-key"; // This key needs to be at least 256 bits long.
//        identity.UseAdminUserProvider();
//    });

//    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

//    elsa.UseWorkflowsApi();
//    elsa.UseRealTimeWorkflows();
//});
#endregion

//builder.Services.AddSingleton<ITenantServiceProvider, TenantServiceProvider>();

builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin() // For demo purposes only. Use a specific origin instead.
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("x-elsa-workflow-instance-id")));
builder.Services.AddHealthChecks();

builder.Services.AddTenantServiceProvider();


//builder.Services.BuildServiceProvider();
//var a1 = builder.Build();

//Task.Delay(20000).Wait();

//a1.Start();

//Task.Delay(20000).Wait();
//var builder2 = WebApplication.CreateBuilder(args);
//var app = builder2.Build();
var app = builder.Build();
//app.UseCors();
//app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();
//app.UseWorkflowsApi();
//app.UseWorkflows();
//app.UseWorkflowsSignalRHubs();

//app.UseMiddleware<TenantMiddleware>(app);

//app.UseAuthentication();
//app.UseAuthorization();
//app.UseWorkflowsApi();
//app.UseWorkflows();
//app.UseWorkflowsSignalRHubs();

//app.UseWhen(context => {
//    var hostname = context.Request.Headers.Origin;
//    var a = new List<string>() { "https://localhost:6002", "https://localhost:6001" };
//    //return a.Contains(hostname);
//    return false;
//}, branch =>
//{

//    branch.UseAuthentication();
//    branch.UseAuthorization();
//    branch.UseWorkflowsApi();
//    branch.UseWorkflows();
//    branch.UseWorkflowsSignalRHubs();
//});

//app.Use(async (context, next) =>
//{
//    var hostname = context.Request.Headers.Origin;
//    var a = new List<string>() { "https://localhost:6002", "https://localhost:6001" };
//    if (a.Contains(hostname))
//    {
//        app.UseAuthentication();
//        app.UseAuthorization();
//        app.UseWorkflowsApi();
//        app.UseWorkflows();
//        app.UseWorkflowsSignalRHubs();
//    }

//    await next(context);
//});

app.UseTenantBuilder();


app.Run();
