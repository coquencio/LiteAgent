using ChatConsoleApp;
using LiteAgent.Connectors;
using LiteAgent.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<GreetPlugins>();
builder.Services.AddSingleton<InventoryPlugins>();

builder.Services.AddAzureOpenAILiteClient(
    "your api key",
    "gpt-4o-mini",
    "https://resource.openai.azure.com/openai/v1/"
);

builder.Services.AddLiteAgent(config =>
{
    config.AddPlugin<GreetPlugins>();
    config.AddPlugin<InventoryPlugins>();
    config.SetTemperature(0.7f);
    config.SetMaxTokens(1000);
});

var app = builder.Build();


var agent = app.Services.GetRequiredService<LiteOrchestratorAgent>();
agent.AddContext("You love to crack some silly jokes when returning final answers to the user");
string response = await agent.SendMessageAsync("Get Jorge last name and with that get his inventory", true);
Console.WriteLine(response);