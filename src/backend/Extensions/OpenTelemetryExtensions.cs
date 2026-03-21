using ContosoTravelAgent.Host.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ContosoTravelAgent.Host.Extensions;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddOpenTelemetryLogging(
        this WebApplicationBuilder builder,
        ContosoTravelAppConfig config)
    {
        var otlpEndpoint = config.OtelExporterOtlpEndpoint ?? "http://localhost:4317";
        
        // Only add Application Insights if connection string is provided
        if (!string.IsNullOrEmpty(config.ApplicationInsightsConnectionString))
        {
            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = config.ApplicationInsightsConnectionString;
            });
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: Constants.ApplicationId, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(Constants.ApplicationId)
                .AddSource("Microsoft.Agents.AI")
                .AddSource("Microsoft.Extensions.AI")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Microsoft.Agents.AI")
                .AddMeter("Microsoft.Extensions.AI")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                }));

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(otlpEndpoint);
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });

        return builder;
    }
}
