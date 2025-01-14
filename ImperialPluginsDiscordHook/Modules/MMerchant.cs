using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImperialPlugins;
using ImperialPluginsDiscordHook.Enum;
using ImperialPluginsDiscordHook.Models;
using ImperialPluginsDiscordHook.Services;

namespace ImperialPluginsDiscordHook.Modules;

[Group("merchant", "Merchant commands.")]
public class MMerchant : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LoggingService _loggingService;
    private readonly IpManagerService _ipManagerService;
    private readonly InteractionService _interactionService;
    private readonly ImperialPluginsClient _imperialPluginsClient;
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly MySqlDbService _dbService;

    public MMerchant(LoggingService loggingService, IpManagerService ipManagerService, InteractionService interactionService, ImperialPluginsClient imperialPluginsClient, DiscordSocketClient discordSocketClient, MySqlDbService dbService)
    {
        _loggingService = loggingService;
        _ipManagerService = ipManagerService;
        _interactionService = interactionService;
        _imperialPluginsClient = imperialPluginsClient;
        _discordSocketClient = discordSocketClient;
        _dbService = dbService;

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
    public async Task GetCustomerServers(string user)
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
        } catch (Exception e)
        {
            await RespondAsync(e.Message, ephemeral: true);
        }
    }
    
    [SlashCommand("initialize", "Initializes yourself as merchant.")]
    public async Task Initialize()
    {
        //await new MySqlDbService(Context.Configuration, _loggingService).InitializeMerchantAsync(settings);
        await _dbService.InitializeMerchantAsync(new MMerchantSettings
        {
            DicordId = Context.User.Id,
        });
        await RespondAsync("Initialized.", ephemeral: true);
    }
    
    [SlashCommand("settings", "Gets your merchant settings.")]
    public async Task Settings()
    {
        var set = _dbService.GetMerchantSettingsAsync(Context.User.Id);
        var postSet = set.Result;
        if (set == null)
        {
            await RespondAsync("You are not initialized, please use the command `/merchant Initialize`", ephemeral: true);
            return;
        }
        
        var embedToSend = new EmbedBuilder()
            .WithTitle("Merchant Settings")
            .AddField("ImperialPlugins ID", postSet.ImperialPluginsId)
            .AddField("Promotions List Enabled", postSet.PromotionsListEnabled)
            .WithColor(Color.Blue);
        
        await RespondAsync(embed: embedToSend.Build(), ephemeral: true);
    }
}