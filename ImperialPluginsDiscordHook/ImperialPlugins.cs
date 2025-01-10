using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using ImperialPlugins;
using ImperialPluginsDiscordHook.Enum;
using ImperialPluginsDiscordHook.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImperialPluginsDiscordHook;

public class ImperialPlugins
{
    private static void Main() => new ImperialPlugins().MainAsync().GetAwaiter().GetResult();

    private async Task MainAsync()
    {
        var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddYamlFile("configuration.yaml")
                .Build();

        using var host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) => services
                .AddSingleton(config)
                .AddSingleton<LoggingService>()
                .AddSingleton<InteractionProviderService>()
                .AddSingleton<ImperialPluginsClient>()
                .AddSingleton<IpManagerService>()
                .AddSingleton<InteractionProviderService>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton(new CommandService(new CommandServiceConfig()))
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.All,
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 500
                })))
                .Build();

            await RunAsync(host);
        }

        private async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateAsyncScope();
            var provider = serviceScope.ServiceProvider;
            
            var client = provider.GetRequiredService<DiscordSocketClient>();
            var commands = provider.GetRequiredService<InteractionService>();
            var config = provider.GetRequiredService<IConfigurationRoot>();
            var imperialPluginsClient = provider.GetRequiredService<ImperialPluginsClient>();
            var ipManagerService = provider.GetRequiredService<IpManagerService>();
            var interactionProviderService = provider.GetRequiredService<InteractionProviderService>();
            var loggingService = provider.GetRequiredService<LoggingService>();


            if (!imperialPluginsClient.IsLoggedIn)
            {
                await loggingService.LogVerbose(ELogType.Error, "Not logged into ImperialPlugins. Exiting.");
                await Task.Delay(5000);
            }
            
            
            client.Ready += async () =>
            {
                await commands.RegisterCommandsGloballyAsync();
                
                await loggingService.LogVerbose(ELogType.Info, $"Discord Client logged in as {client.CurrentUser.Username}#{client.CurrentUser.Discriminator} ({client.CurrentUser.Id}) with {commands.SlashCommands.Count} Slash Commands(s)");
                await client.SetActivityAsync(new Game("ImperialPlugins", ActivityType.Watching));
            };

            await client.LoginAsync(TokenType.Bot, config["client:token"]);
            await client.StartAsync();

            await Task.Delay(-1);
        }
}