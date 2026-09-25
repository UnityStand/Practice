using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Events.Api.DependencyInjection;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder, string serviceName)
    {
        var otlpEndpoint = new Uri(builder.Configuration["Otlp:Endpoint"]!);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = otlpEndpoint))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return builder;
    }
}
