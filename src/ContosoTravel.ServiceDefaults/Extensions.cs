using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for configuring common .NET Aspire services.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds service defaults to the host builder.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();
            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry for the application.
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            // Metrics sending commented out
            // .WithMetrics(metrics =>
            // {
            //     metrics.AddAspNetCoreInstrumentation()
            //         .AddHttpClientInstrumentation()
            //         .AddRuntimeInstrumentation();
            // })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation 
                    // (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    // .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry for the application with custom sources, meters, and observability settings.
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(
        this TBuilder builder,
        string serviceName,
        string serviceVersion,
        string? otlpEndpoint = null,
        string? applicationInsightsConnectionString = null,
        string[]? additionalSources = null,
        string[]? additionalMeters = null) where TBuilder : IHostApplicationBuilder
    {
        var useOtlp = !string.IsNullOrEmpty(otlpEndpoint ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        var useAppInsights = !string.IsNullOrEmpty(applicationInsightsConnectionString ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

        var otelBuilder = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion));

        // Configure tracing
        otelBuilder.WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(serviceName);

            // Add additional sources (e.g., Microsoft.Agents.AI, Microsoft.Extensions.AI)
            if (additionalSources != null)
            {
                foreach (var source in additionalSources)
                {
                    tracing.AddSource(source);
                }
            }

            // Add OTLP exporter if configured
            if (useOtlp)
            {
                var endpoint = otlpEndpoint ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(endpoint!);
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
            }
        });

        // Configure metrics - Metrics sending commented out
        // otelBuilder.WithMetrics(metrics =>
        // {
        //     metrics
        //         .AddAspNetCoreInstrumentation()
        //         .AddHttpClientInstrumentation()
        //         .AddRuntimeInstrumentation();

        //     // Add additional meters (e.g., Microsoft.Agents.AI, Microsoft.Extensions.AI)
        //     if (additionalMeters != null)
        //     {
        //         foreach (var meter in additionalMeters)
        //         {
        //             metrics.AddMeter(meter);
        //         }
        //     }

        //     // Add OTLP exporter if configured
        //     if (useOtlp)
        //     {
        //         var endpoint = otlpEndpoint ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        //         metrics.AddOtlpExporter(options =>
        //         {
        //             options.Endpoint = new Uri(endpoint!);
        //             options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        //         });
        //     }
        // });

        // Configure logging
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;

            // Add OTLP exporter if configured
            if (useOtlp)
            {
                var endpoint = otlpEndpoint ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                options.AddOtlpExporter(exporterOptions =>
                {
                    exporterOptions.Endpoint = new Uri(endpoint!);
                    exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
            }
        });

        // Add Azure Monitor/Application Insights if configured
        if (useAppInsights)
        {
            var connString = applicationInsightsConnectionString ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            otelBuilder.UseAzureMonitor(options =>
            {
                options.ConnectionString = connString;
            });
        }

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Enable the Azure Monitor exporter if connection string is available
        if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry()
               .UseAzureMonitor();
        }

        return builder;
    }

    /// <summary>
    /// Adds default health checks.
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps default health check endpoints.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments 
        // has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these 
        // endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
