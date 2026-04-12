using ChatConsoleApp;
using LiteAgent.Connectors;
using LiteAgent.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAzureOpenAILiteClient("youApiKey", "deploymentName", "endpoint");

builder.Services.AddLiteAgent(new GreetPlugins(), new InventoryPlugins());

var app = builder.Build();


var agent = app.Services.GetRequiredService<LiteOrchestratorAgent>();

agent.Configure(temperature: 0.5f, maxTokens: 800);

agent.RegisterTools(new GreetPlugins());

string response = await agent.SendMessageAsync("Hello, use the greet plugin");
Console.WriteLine(response);