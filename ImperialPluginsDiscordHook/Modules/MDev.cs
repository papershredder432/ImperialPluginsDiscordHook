using System.ComponentModel;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImperialPlugins;
using ImperialPluginsDiscordHook.Enum;
using ImperialPluginsDiscordHook.Services;

namespace ImperialPluginsDiscordHook.Modules;

[Group("dev", "Dev commands.")]
public class MDev : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LoggingService _loggingService;
    private readonly IpManagerService _ipManagerService;
    private readonly InteractionService _interactionService;
    private readonly ImperialPluginsClient _imperialPluginsClient;
    private readonly DiscordSocketClient _discordSocketClient;

    public MDev(LoggingService loggingService, IpManagerService ipManagerService, InteractionService interactionService, ImperialPluginsClient imperialPluginsClient, DiscordSocketClient discordSocketClient)
    {
        _loggingService = loggingService;
        _ipManagerService = ipManagerService;
        _interactionService = interactionService;
        _imperialPluginsClient = imperialPluginsClient;
        _discordSocketClient = discordSocketClient;

        _discordSocketClient.SelectMenuExecuted += SelectMenuExecuted;
    }

    private async Task SelectMenuExecuted(SocketMessageComponent component)
    {
        try
        {
            var interaction = (IComponentInteraction) component;
            var user = component.Data.CustomId.Replace("server_menu_select_", "");
            
            var ipUser = _ipManagerService.GetUserAsync(user);
            if (ipUser == null)
            {
                await RespondAsync("User not found.", ephemeral: true);
                return;
            }
        
            var servers = _ipManagerService.GetCustomerServers(ipUser);
            if (servers == null)
            {
                await RespondAsync("No servers found.", ephemeral: true);
                return;
            }
            
            var serverId = component.Data.Values.FirstOrDefault().Replace("server_menu_", "");
            var selectedServer = servers.FirstOrDefault(x => x.id == int.Parse(serverId));
            
            var embedToSend = new EmbedBuilder()
                .WithTitle(selectedServer.serverName)
                .WithDescription($"{selectedServer.Host}")
                .WithColor(Color.Blue)
                .AddField("Port", selectedServer.Port)
                .AddField("Whitelist Status", selectedServer.isWhitelisted)
                .AddField("First Seen", selectedServer.registrationTime)
                .AddField("Last Seen", selectedServer.lastActivityTime)
                .AddField("Whitelist Time", selectedServer.whitelistTime != null ? selectedServer.whitelistTime : "N/A")
                .AddField("Products", /*string.Join(", ", serverProductNames)*/"TODO", true)
                .WithFooter(user);

            if (component.HasResponded) 
                await _loggingService.LogVerbose(ELogType.Debug, "Component already responded.");
            
            await component.RespondAsync(embed: embedToSend.Build(), ephemeral: true);
        } catch (Exception e)
        {
            await RespondAsync(e.Message, ephemeral: true);
        }
    }

    [SlashCommand("getcustomerservers", "Gets all servers of a customer.")]
    public async Task FakeTicket(string user)
    {
        var ipUser = _ipManagerService.GetUserAsync(user);
        if (ipUser == null)
        {
            await RespondAsync("User not found.", ephemeral: true);
            return;
        }
        
        var servers = _ipManagerService.GetCustomerServers(ipUser);
        if (servers == null)
        {
            await RespondAsync("No servers found.", ephemeral: true);
            return;
        }
        
        HashSet<int> serverProductIds = new HashSet<int>();
        HashSet<string?> serverProductNames = new HashSet<string?>();
        foreach (var server in servers)
            serverProductIds.Add(server.productRegistrationId);
        //foreach (var product in serverProductIds)
        //    serverProductNames.Add(_imperialPluginsClient.GetOwnPluginsAsync(1000).Result.Items.FirstOrDefault(x => x.ID == product)?.Name);

        //await _loggingService.LogVerbose(ELogType.Debug, string.Join(", ", serverProductNames.ToList()));
        
        try
        {

            /*
            var serverNum = 0;
            var embedToSend = new EmbedBuilder()
                .WithTitle($"{ipUser.UserName}'s Servers")
                .WithDescription($"Servers {serverNum+1}/{servers.Count}")
                .WithColor(Color.Blue)
                .AddField("Server ID", servers[0].id, true)
                .AddField("Server Address:Port", $"{servers[0].Host}:{servers[0].Port}", true)
                .AddField("Whitelist Status", servers[0].isWhitelisted, true)
                .AddField("First Seen", servers[0].registrationTime, true)
                .AddField("Last Seen", servers[0].lastActivityTime, true)
                .AddField("Whitelist Time", servers[0].whitelistTime != null ? servers[0].whitelistTime : "N/A", true)
                .AddField("Products", string.Join(", ", serverProductNames), true);
            */
            
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder($"Select a server.")
                .WithCustomId($"server_menu_select_{user}");
            var serverNum = 0;
            foreach (var server in servers)
            {
                serverNum++;
                menuBuilder.AddOption($"[{serverNum}] Server {server.id}", $"server_menu_{server.id}", $"{server.Host}:{server.Port}");
            }
            
            await RespondAsync(components: new ComponentBuilder().WithSelectMenu(menuBuilder).Build(), ephemeral: true);


            /*
            var embedsNeeded = Math.DivRem(servers.Count*7, 21, out var remainder);
            if (remainder > 0) embedsNeeded++;
            var fieldsCount = 0;
            await _loggingService.LogVerbose(ELogType.Debug, $"Expecting {embedsNeeded} embeds. {embedsNeeded-1} \"full\" embeds and {remainder/7} servers in the last embed.");

            var embedsToSend = new List<Embed>();
            for (int i = 0; i < embedsNeeded; i++)
            {
                await _loggingService.LogVerbose(ELogType.Debug, $"Creating embed {i+1}/{embedsNeeded}.");

                var embedToSend = new EmbedBuilder()
                    .WithTitle($"{ipUser.UserName}'s Servers")
                    .WithDescription($"Servers /{servers.Count}")
                    .WithColor(Color.Blue);

                for (int j = 0; j < 3; j++)
                {
                    await _loggingService.LogVerbose(ELogType.Debug, $"Creating fields {(j+1)*7}/21");
                    fieldsCount += 7;

                    embedToSend
                        .AddField("Server ID", servers[fieldsCount%7].id, true)
                        .AddField("Server Address:Port", $"{servers[fieldsCount%7].Host}:{servers[fieldsCount%7].Port}", true)
                        .AddField("Whitelist Status", servers[fieldsCount%7].isWhitelisted, true)
                        .AddField("First Seen", servers[fieldsCount%7].registrationTime, true)
                        .AddField("Last Seen", servers[fieldsCount%7].lastActivityTime, true)
                        .AddField("Whitelist Time", servers[fieldsCount%7].whitelistTime != null ? servers[fieldsCount%7].whitelistTime : "N/A", true)
                        .AddField("Products", string.Join(", ", serverProductNames), true);
                }

                embedsToSend.Add(embedToSend.Build());
            }
            await _loggingService.LogVerbose(ELogType.Debug, $"Attempting to send {embedsToSend.Count} embeds.");
            await RespondAsync(embeds: embedsToSend.ToArray(), ephemeral: true);
            */
        } catch (Exception e)
        {
            await RespondAsync(e.Message, ephemeral: true);
        }
    }
}