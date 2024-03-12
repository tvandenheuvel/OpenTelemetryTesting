using Npgsql;
using OpenTelemetry.Shims.OpenTracing;
using OpenTelemetry.Trace;
using OpenTelemetry.Context.Propagation;
using OpenTracing;
using OpenTracing.Util;
using System.Reflection;
using SubLibrary;
using OpenTelemetry.Resources;

namespace OpenTelemetryTesting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCustomTracing();
            builder.Services.AddTransient<ISubClassWithOpenTracing, SubClassWithOpenTracing>();

            var app = builder.Build();

            app.MapGet("/", () =>
            {
                var sc = app.Services.GetRequiredService<ISubClassWithOpenTracing>();

                return sc.ParentSpan("Hello World!");
            });

            app.Run();
        }
    }

    public static class Tracing
    {
        public static IServiceCollection AddCustomTracing(this IServiceCollection services)
        {
            // Get the datadog uri for this instance.
            Uri traceAgentUrl = new Uri($"http://localhost:8126/");

            services.AddOpenTelemetry().WithTracing(tcb =>
            {
                tcb.AddSource(Assembly.GetExecutingAssembly().GetName().Name)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Assembly.GetExecutingAssembly().GetName().Name))
                 .AddAspNetCoreInstrumentation(options =>
                 {
                     //options.Filter = (httpContext) =>
                     //{
                     //    // Ignore requests to the any configured probes.
                     //    return !httpContext.Request.Path.StartsWithSegments("/probes", StringComparison.OrdinalIgnoreCase);
                     //};
                 })
                 .AddNpgsql()
                 .AddHttpClientInstrumentation(options =>
                 {
                     //options.FilterHttpRequestMessage = (request) =>
                     //{
                     //    // Ignore http post to DD agent.
                     //    return request.RequestUri.Host != traceAgentUrl.Host ||
                     //        request.RequestUri.Port != traceAgentUrl.Port;
                     //};
                 })
                 .AddConsoleExporter()
                 ;
            });

            services.AddSingleton<Tracer>(serviceProvider => TracerProvider.Default.GetTracer(Assembly.GetExecutingAssembly().GetName().Name));

            //// Adds the ITracer, for old tracing code.
            services.AddSingleton<ITracer>(serviceProvider =>
            {
                var tracer = new TracerShim(TracerProvider.Default, Propagators.DefaultTextMapPropagator);

                GlobalTracer.RegisterIfAbsent(tracer);

                return tracer;
            });

            return services;
        }
    }
}
