// Lab 08: Agent Skills - File-Based Skills with Progressive Disclosure
// Learn how to use FileAgentSkillsProvider to load modular skill packages from SKILL.md files

// Add NuGet package references using file-based app syntax (#:package Name@Version)
#:package Azure.AI.OpenAI@2.1.0
#:package Azure.Identity@1.21.0
#:package Microsoft.Agents.AI@1.1.0
#:package Microsoft.Agents.AI.Abstractions@1.1.0
#:package Microsoft.Extensions.AI@10.5.0
#:package Microsoft.Extensions.AI.OpenAI@10.5.0
#:package DotNetEnv@3.1.1
#:package OpenTelemetry@1.15.3
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.15.3
#:package OpenTelemetry.Extensions.Hosting@1.15.3
#:package Microsoft.Extensions.Logging@10.0.0
#:package Microsoft.Extensions.Logging.Console@10.0.0
#:package Microsoft.Extensions.DependencyInjection@10.0.0

#pragma warning disable MAAI001 // FileAgentSkillsProvider is experimental

using System.ClientModel;
using System.ComponentModel;
using Azure.AI.OpenAI;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string SourceName = "TravelAssistant";
const string ServiceName = "TravelAssistant";

// Configure JSON serialization for Azure SDK compatibility with .NET 10
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

// Step 1: Load environment variables
LoadEnv();

// Step 2: Initialize OpenTelemetry
var (loggerFactory, appLogger, tracerProvider) = InitTelemetry(ServiceName);

// Step 3: Create chat client
// チャットクライアントを作成
var chatClient = CreateChatClient(appLogger);
if (chatClient == null)
{
    tracerProvider.Dispose();
    return;
}

// Step 4: Skills Provider - Discovers skills from the 'skills' directory
// Skills Provider を作成し 'skills' ディレクトリからスキルを検出します

// FileAgentSkillsProvider implements progressive disclosure:
//   1. Advertise - skills are advertised with name + description (~100 tokens per skill)
//   2. Load - full instructions loaded on-demand via load_skill tool
//   3. Read resources - supplementary files loaded via read_skill_resource tool
// FileAgentSkillsProvider は段階的開示を実装しています:
//   1. Advertise - 名前と説明でスキル概要を提示（スキルあたり約 100 トークン）
//   2. Load - load_skill ツールで必要時に詳細指示を読み込み
//   3. Read resources - read_skill_resource ツールで補助ファイルを読み込み
var skillsProvider = new AgentSkillsProvider(
    skillPath: Path.Combine(Directory.GetCurrentDirectory(), "labs/00-foundations/lab08-skills/skills"));

appLogger.LogInformation("FileAgentSkillsProvider created, discovering skills from ./skills directory");

// Step 5: Create agent with skills and tools
// スキルとツール付きのエージェントを作成
var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TravelAssistant",
    ChatOptions = new()
    {
        Instructions = "あなたは親切な旅行アシスタントです。天気情報、ビザ要件、目的地の提案を活用して旅行計画を支援してください。",
        Tools = [AIFunctionFactory.Create(GetWeatherForecast)],
    },
    AIContextProviders = [skillsProvider],
})
.AsBuilder()
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
.UseLogging(loggerFactory)
.Build();

appLogger.LogInformation("Agent created with file-based skills successfully");

// Step 6: Demonstrate agent using skills (progressive disclosure pattern)
// スキル活用の動作（段階的開示パターン）を表示
try
{
    AgentSession session = await agent.CreateSessionAsync();

    appLogger.LogInformation("=== Travel Assistant with File-Based Skills ===");

    // // Example 1: Weather inquiry (agent will load weather-info skill)
    // // 例 1: 天気の問い合わせ（weather-info スキルを読み込み）
    // appLogger.LogInformation("Example 1: Checking weather for destination");
    // appLogger.LogInformation("--------------------------------------------");
    // var query1 = "9月にメルボルンへ行きます。天気はどんな感じで、何を持っていくべきですか？";
    // appLogger.LogInformation("User: {Query}", query1);
    // var response1 = await agent.RunAsync(query1, session);
    // appLogger.LogInformation("Agent: {Response}", response1.Text);

    // // Example 2: Visa requirements (agent will load visa-assistance skill)
    // // 例 2: ビザ要件（visa-assistance スキルを読み込み）
    // appLogger.LogInformation("");
    // appLogger.LogInformation("Example 2: Visa requirements");
    // appLogger.LogInformation("----------------------------");
    // var query2 = "私は日本人で、アメリカに2週間、その後カナダに1週間行く予定です。どのビザが必要ですか？";
    // appLogger.LogInformation("User: {Query}", query2);
    // var response2 = await agent.RunAsync(query2, session);
    // appLogger.LogInformation("Agent: {Response}", response2.Text);

    // Example 3: Destination recommendation (agent will load trip-planner skill)
    // 例 3: 目的地提案（trip-planner スキルを読み込み）
    // appLogger.LogInformation("");
    // appLogger.LogInformation("Example 3: Destination recommendations");
    // appLogger.LogInformation("--------------------------------------");
    // var query3 = "一人旅です。オーストラリアで食事がおいしくて治安の良い場所を探しています。予算は中程度です。おすすめはありますか？";
    // appLogger.LogInformation("User: {Query}", query3);
    // var response3 = await agent.RunAsync(query3, session);
    // appLogger.LogInformation("Agent: {Response}", response3.Text);

    // Example 4: Multi-skill query (combines weather, visa, and trip-planner skills)
    // 例 4: 複合問い合わせ（weather / visa / trip-planner を併用）
    appLogger.LogInformation("");
    appLogger.LogInformation("Example 4: Combined travel planning");
    appLogger.LogInformation("------------------------------------");
    var query4 = "3月にニュージーランドへ行きたいです。天気はどうですか？日本人の場合ビザは必要ですか？必見スポットも教えてください。";
    appLogger.LogInformation("User: {Query}", query4);
    var response4 = await agent.RunAsync(query4, session);
    appLogger.LogInformation("Agent: {Response}", response4.Text);

    appLogger.LogInformation("Agent response completed");
}
catch (Exception ex)
{
    appLogger.LogError(ex, "Agent interaction failed: {ErrorMessage}", ex.Message);
}
finally
{
    tracerProvider.Dispose();
}


// ==================== TOOLS ====================

[Description("Get the weather forecast for a specific city")]
static string GetWeatherForecast([Description("The city name to get weather for")] string city)
{
    var conditions = new[] { "Sunny", "Partly Cloudy", "Cloudy", "Rainy", "Stormy", "Snowy", "Foggy" };
    var random = new Random();
    var temp = random.Next(-5, 40);
    var condition = conditions[random.Next(conditions.Length)];
    var humidity = random.Next(30, 90);

    return $"Weather in {city}: {condition}, {temp}C, Humidity: {humidity}%";
}

// ==================== HELPER FUNCTIONS ====================

void LoadEnv()
{
    var currentDir = Directory.GetCurrentDirectory();
    while (currentDir != null)
    {
        var azureYaml = Path.Combine(currentDir, "azure.yaml");
        if (File.Exists(azureYaml))
        {
            var envFile = Path.Combine(currentDir, ".env");
            if (File.Exists(envFile))
            {
                Env.Load(envFile);
                return;
            }
        }
        currentDir = Directory.GetParent(currentDir)?.FullName;
    }
}

IChatClient? CreateChatClient(ILogger appLogger)
{
    var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_ENDPOINT");
    var azureApiKey = Environment.GetEnvironmentVariable("AZURE_AI_SERVICES_KEY");
    var modelName = Environment.GetEnvironmentVariable("AZURE_TEXT_MODEL_NAME") ?? "gpt-4o";

    var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    var githubModelId = Environment.GetEnvironmentVariable("GITHUB_TEXT_MODEL_ID") ?? "gpt-4o";
    var githubBaseUrl = Environment.GetEnvironmentVariable("GITHUB_MODELS_BASE_URL") ?? "https://models.inference.ai.azure.com";

    if (!string.IsNullOrEmpty(azureEndpoint) && !string.IsNullOrEmpty(azureApiKey))
    {
        appLogger.LogInformation("Using Azure OpenAI with model: {ModelName}", modelName);
        var azureClient = new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(azureApiKey));
        return azureClient.GetChatClient(modelName)
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();
    }
    else if (!string.IsNullOrEmpty(githubToken))
    {
        appLogger.LogInformation("Using GitHub Models with model: {ModelId}", githubModelId);
        var githubClient = new AzureOpenAIClient(new Uri(githubBaseUrl), new ApiKeyCredential(githubToken));
        return githubClient.GetChatClient(githubModelId)
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
            .Build();
    }
    else
    {
        appLogger.LogError("No valid credentials found.");
        return null;
    }
}

(ILoggerFactory, ILogger<Program>, TracerProvider) InitTelemetry(string serviceName)
{
    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

    var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: "1.0.0"))
        .AddSource(SourceName)
        .AddSource("Microsoft.Agents.AI")
        .AddSource("Microsoft.Extensions.AI")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
        .Build();

    var serviceCollection = new ServiceCollection();
    serviceCollection.AddLogging(loggingBuilder => loggingBuilder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole()
        .AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion: "1.0.0"));
            options.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri(otlpEndpoint));
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        }));

    var serviceProvider = serviceCollection.BuildServiceProvider();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var appLogger = loggerFactory.CreateLogger<Program>();

    return (loggerFactory, appLogger, tracerProvider);
}
