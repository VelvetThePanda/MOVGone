using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Hosting.Extensions;

var host = Host.CreateDefaultBuilder();

host.ConfigureAppConfiguration(c => c.AddEnvironmentVariables("MOV_"));

host.AddDiscordService(s => s.GetService<IConfiguration>().GetValue<string>("BOT_TOKEN"));

host.ConfigureServices((host, services) =>
{
    var apiUrl = host.Configuration.GetValue<string>("API_URL") ?? "localhost";
    services.AddHttpClient("ApiClient", (client) => client.BaseAddress = new(apiUrl));
});

var app = host.UseConsoleLifetime().Build();

await app.RunAsync();