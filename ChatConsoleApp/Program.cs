using ChatConsoleApp;
using LiteAgent.Connectors;
using LiteAgent.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAzureOpenAILiteClient(
    "your key",
    "gpt-4o-mini",
    "https://resource.openai.azure.com/openai/v1/"
);

builder.Services.AddLiteAgent(new GreetPlugins(), new InventoryPlugins());

var app = builder.Build();


var agent = app.Services.GetRequiredService<LiteOrchestratorAgent>();
agent.AddContext("You love to crack some silly jokes when returning final answers to the user");

string response = await agent.SendMessageAsync("Give me the inventory on furniture category");
Console.WriteLine(response);