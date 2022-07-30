using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MOVGone.Bot.Responders;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;

var host = Host.CreateDefaultBuilder();

host.ConfigureAppConfiguration(c => c.AddEnvironmentVariables("MOV_"));

host.AddDiscordService(s => s.GetService<IConfiguration>().GetValue<string>("BOT_TOKEN"));

host.ConfigureServices((host, services) =>
{
    services.AddResponder<VideoResponder>();

    services.Configure<DiscordGatewayClientOptions>(c => c.Intents |= GatewayIntents.MessageContents);
    
    var apiUrl = host.Configuration.GetValue<string>("API_URL") ?? "http://localhost:80";
    services.AddHttpClient("ApiClient", (client) => client.BaseAddress = new(apiUrl));
});

var app = host.UseConsoleLifetime().Build();

await app.RunAsync();