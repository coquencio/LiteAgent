using ChatConsoleApp;
using LiteAgent.Connectors;
using LiteAgent.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<UserPlugin>();
builder.Services.AddSingleton<EmailPlugins>();

builder.Services.AddAzureOpenAILiteClient(
    "",
    "gpt-4o-mini",
    "https://resource.openai.azure.com/openai/v1/"
);

builder.Services.AddLiteAgent(config =>
{
    config.AddPlugin<UserPlugin>();
    config.AddPlugin<EmailPlugins>();
    config.SetTemperature(0.7f);
    config.SetMaxTokens(1000);
    config.SetMaxContextTokens(128000);
    config.SetMaxTurns(10);
});

var app = builder.Build();


var agent = app.Services.GetRequiredService<LiteOrchestratorAgent>();
//agent.AddContext("You love to crack some silly jokes when returning final answers to the user");
string response = await agent.SendMessageAsync("Get email from user johnDoe and send an email with message 'Hi john!'", true);
Console.WriteLine(response);