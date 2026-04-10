using ChatConsoleApp;
using LiteAgent.Connectors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ILiteClient>(sp =>
    new LiteAzureOpenAIClient(
        apiKey: "your-azure-api-key",
        deployment: "gpt-4o-mini",
        endpoint: "https://{yourProjectName}-resource.openai.azure.com"
    ));

builder.Services.AddTransient<LiteOrchestratorAgent>();

var app = builder.Build();


var agent = app.Services.GetRequiredService<LiteOrchestratorAgent>();

agent.Configure(temperature: 0.5f, maxTokens: 800);

agent.RegisterTools(new Plugins());

string response = await agent.SendMessageAsync("Hello, use the greet plugin");
Console.WriteLine(response);