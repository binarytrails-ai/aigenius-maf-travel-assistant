# Setting Up Aspire Dashboard Locally

The .NET Aspire Dashboard is a web-based UI for viewing OpenTelemetry traces, metrics, and logs in real-time. This guide will help you set up the dashboard locally for viewing telemetry data from your application.

---

## Prerequisites

1. **Docker**: The Aspire Dashboard runs in a Docker container. You need to have Docker installed on your machine.

    - [Docker Desktop for Windows](https://docs.docker.com/desktop/install/windows-install/)
    - [Docker Desktop for Mac](https://docs.docker.com/desktop/install/mac-install/)

    If you are using GitHub Codespaces, Docker is available in the environment.

---

## Setup Instructions

1. Open a terminal and pull the latest Aspire Dashboard Docker image:

    ```bash
    docker pull mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ```

2. Run the Aspire Dashboard container:

    ```bash
    docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ```

    This command will start the dashboard and map the necessary ports for the web UI and OTLP endpoint. 
    
    The environment variable `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` allows you to access the dashboard without authentication, which is useful for local development and GitHub Codespaces.

3. Access the dashboard:

    - If you are running locally, open your web browser and navigate to `http://localhost:18888`.
    - If you are using GitHub Codespaces, go to the **Ports** panel in VS Code, find port **18888**, and click the **globe icon** (🌐) to open the dashboard in your browser.

## Aspire Dashboard Overview

The dashboard has three main tabs:

1. **Traces**: View distributed traces across your application, drill down into individual spans, analyze execution flow and performance, and view LLM prompts and tool calls (when sensitive data is enabled).

    ![Aspire Dashboard - Trace View](../media/aspire-trace-view.png)

2. **Metrics**: Monitor application performance metrics, track resource utilization, and view custom metrics from your agents.
3. **Logs**: View structured logs from your application, filter and search log entries.

    ![Aspire Dashboard - Span Details](../media/aspire-trace-view-log-entry.png)

---

## Additional Resources

- [.NET Aspire Dashboard Documentation](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry Protocol Specification](https://opentelemetry.io/docs/specs/otlp/)

---