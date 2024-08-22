using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using FastEndpoints;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElsaWebMultiDb
{
    public static class CustomizeExtensionBaseOnElsa
    {
        /// <summary>
        /// Register the FastEndpoints middleware configured for use with with Elsa API endpoints.
        /// </summary>
        /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to register the endpoints with.</param>
        /// <param name="routePrefix">The route prefix to apply to Elsa API endpoints.</param>
        /// /// <example>E.g. "elsa/api" will expose endpoints like this: "/elsa/api/workflow-definitions"</example>
        public static IEndpointRouteBuilder MapWorkflowsApiCustomized(this IEndpointRouteBuilder routes, string routePrefix = "elsa/api") =>
            routes.MapFastEndpoints(config =>
            {
                var tag = Guid.NewGuid().ToString().Split('-')[0];
                config.Endpoints.Configurator = (e =>
                {
                    if (e.EndpointTags == null || e.EndpointTags.Count == 0)
                    {
                        e.Tags(tag);
                    }
                });
                config.Endpoints.RoutePrefix = routePrefix;
                config.Endpoints.PrefixNameWithFirstTag = true;
                config.Serializer.RequestDeserializer = DeserializeRequestAsync;
                config.Serializer.ResponseSerializer = SerializeRequestAsync;
            });

        private static ValueTask<object?> DeserializeRequestAsync(HttpRequest httpRequest, Type modelType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
        {
            var serializer = httpRequest.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
            var options = serializer.CreateOptions();

            return serializerContext == null
                ? JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, options, cancellationToken)
                : JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, serializerContext, cancellationToken);
        }

        private static Task SerializeRequestAsync(HttpResponse httpResponse, object? dto, string contentType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
        {
            var serializer = httpResponse.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
            var options = serializer.CreateOptions();

            httpResponse.ContentType = contentType;
            return serializerContext == null
                ? JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), options, cancellationToken)
                : JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), serializerContext, cancellationToken);
        }


        //public static IEndpointRouteBuilder MapFastEndpointsCustomized(this IEndpointRouteBuilder app, Action<Config>? configAction = null)
        //{
        //    Config.ServiceResolver = app.ServiceProvider.GetRequiredService<IServiceResolver>();
        //    JsonSerializerOptions jsonSerializerOptions = app.ServiceProvider.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value.SerializerOptions;
        //    Config.SerOpts.Options = ((jsonSerializerOptions != null) ? new JsonSerializerOptions(jsonSerializerOptions) : Config.SerOpts.Options);
        //    Config.SerOpts.Options.IgnoreToHeaderAttributes();
        //    Config.BndOpts.AddTypedHeaderValueParsers(Config.SerOpts.Options);
        //    configAction?.Invoke(app.ServiceProvider.GetRequiredService<Config>());
        //    EndpointData requiredService = app.ServiceProvider.GetRequiredService<EndpointData>();
        //    IEndpointFactory requiredService2 = app.ServiceProvider.GetRequiredService<IEndpointFactory>();
        //    AuthorizationOptions value = app.ServiceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        //    using IServiceScope serviceScope = app.ServiceProvider.CreateScope();
        //    DefaultHttpContext ctx2 = new DefaultHttpContext
        //    {
        //        RequestServices = serviceScope.ServiceProvider
        //    };
        //    ConcurrentDictionary<string, int> concurrentDictionary = new ConcurrentDictionary<string, int>();
        //    int num = 0;
        //    StringBuilder builder = new StringBuilder();
        //    EndpointDefinition[] found = requiredService.Found;
        //    foreach (EndpointDefinition endpointDefinition in found)
        //    {
        //        BaseEndpoint instance = requiredService2.Create(endpointDefinition, ctx2);
        //        endpointDefinition.Initialize(instance, ctx2);
        //        if (Config.EpOpts.Filter != null && !Config.EpOpts.Filter(endpointDefinition))
        //        {
        //            continue;
        //        }

        //        if ((!(endpointDefinition.Verbs?.Any())) ?? true)
        //        {
        //            throw new ArgumentException("No HTTP Verbs declared on: [" + endpointDefinition.EndpointType.FullName + "]");
        //        }

        //        if ((!(endpointDefinition.Routes?.Any())) ?? true)
        //        {
        //            throw new ArgumentException("No Routes declared on: [" + endpointDefinition.EndpointType.FullName + "]");
        //        }

        //        Config.EpOpts.Configurator?.Invoke(endpointDefinition);
        //        if (endpointDefinition.AntiforgeryEnabled && (app.ServiceProvider.GetService<IAntiforgery>() == null || !AntiforgeryMiddleware.IsRegistered))
        //        {
        //            throw new InvalidOperationException("AntiForgery middleware setup is incorrect!");
        //        }

        //        AddSecurityPolicy(value, endpointDefinition);
        //        IAuthorizeData[] authorizeData = BuildAuthorizeAttributes(endpointDefinition);
        //        int num2 = 0;
        //        string[] routes = endpointDefinition.Routes;
        //        foreach (string route in routes)
        //        {
        //            string text = builder.BuildRoute(endpointDefinition.Version.Current, route, endpointDefinition.OverriddenRoutePrefix);
        //            IEndpoint.SetTestUrl(endpointDefinition.EndpointType, text);
        //            num2++;
        //            string[] verbs = endpointDefinition.Verbs;
        //            foreach (string text2 in verbs)
        //            {
        //                RouteHandlerBuilder routeHandlerBuilder = app.MapMethods(text, new string[1] { text2 }, (Func<HttpContext, IEndpointFactory, Task>)((HttpContext ctx, [FromServices] IEndpointFactory factory) => RequestHandler.Invoke(ctx, factory)));
        //                Type endpointType = endpointDefinition.EndpointType;
        //                string verb = ((endpointDefinition.Verbs.Length > 1) ? text2 : null);
        //                int? routeNum = ((endpointDefinition.Routes.Length > 1) ? new int?(num2) : null);
        //                List<string>? endpointTags = endpointDefinition.EndpointTags;
        //                routeHandlerBuilder.WithName(endpointType.EndpointName(verb, routeNum, (endpointTags != null && endpointTags.Count > 0) ? endpointDefinition.EndpointTags[0] : null));
        //                routeHandlerBuilder.WithMetadata(endpointDefinition);
        //                if (endpointDefinition.AttribsToForward != null)
        //                {
        //                    routeHandlerBuilder.WithMetadata(endpointDefinition.AttribsToForward.ToArray());
        //                }

        //                endpointDefinition.InternalConfigAction(routeHandlerBuilder);
        //                if (endpointDefinition.AnonymousVerbs?.Contains(text2) ?? false)
        //                {
        //                    routeHandlerBuilder.AllowAnonymous();
        //                }
        //                else
        //                {
        //                    routeHandlerBuilder.RequireAuthorization(authorizeData);
        //                }

        //                if (endpointDefinition.ResponseCacheSettings != null)
        //                {
        //                    routeHandlerBuilder.WithMetadata(endpointDefinition.ResponseCacheSettings);
        //                }

        //                if (endpointDefinition.FormDataContentType != null)
        //                {
        //                    routeHandlerBuilder.Accepts(endpointDefinition.ReqDtoType, endpointDefinition.FormDataContentType);
        //                }

        //                EndpointSummary? endpointSummary = endpointDefinition.EndpointSummary;
        //                if (endpointSummary != null && endpointSummary.ProducesMetas.Count > 0)
        //                {
        //                    EndpointSummary.ClearDefaultProduces200Metadata(routeHandlerBuilder);
        //                    foreach (IProducesResponseTypeMetadata producesMeta in endpointDefinition.EndpointSummary.ProducesMetas)
        //                    {
        //                        routeHandlerBuilder.WithMetadata(producesMeta);
        //                    }
        //                }

        //                endpointDefinition.UserConfigAction?.Invoke(routeHandlerBuilder);
        //                string key = text2 + ":" + text;
        //                concurrentDictionary.AddOrUpdate(key, 1, (string _, int c) => c + 1);
        //                num++;
        //            }

        //            endpointDefinition.AttribsToForward = null;
        //        }
        //    }

        //    app.ServiceProvider.GetRequiredService<ILogger<StartupTimer>>().LogInformation("Registered {@total} endpoints in {@time} milliseconds.", num, requiredService.Stopwatch.ElapsedMilliseconds.ToString("N0"));
        //    requiredService.Stopwatch.Stop();
        //    if (!Config.VerOpts.IsUsingAspVersioning)
        //    {
        //        bool flag = false;
        //        ILogger<DuplicateHandlerRegistration> requiredService3 = app.ServiceProvider.GetRequiredService<ILogger<DuplicateHandlerRegistration>>();
        //        foreach (KeyValuePair<string, int> item in concurrentDictionary)
        //        {
        //            if (item.Value > 1)
        //            {
        //                flag = true;
        //                requiredService3.LogError($"The route \"{item.Key}\" has {item.Value} endpoints registered to handle requests!");
        //            }
        //        }

        //        if (flag)
        //        {
        //            throw new InvalidOperationException("Duplicate routes detected! See log for more details.");
        //        }
        //    }

        //    CommandExtensions.TestHandlersPresent = app.ServiceProvider.GetService<TestCommandHandlerMarker>() != null;
        //    return app;
        //}


    }

}
