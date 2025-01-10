using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ImperialPlugins;
using ImperialPlugins.Models;
using ImperialPlugins.Models.Notifications;
using ImperialPlugins.Models.Plugins;
using ImperialPlugins.Models.Servers;
using ImperialPluginsDiscordHook.Enum;
using Microsoft.Extensions.Configuration;
using Timer = System.Timers.Timer;

namespace ImperialPluginsDiscordHook.Services;

public class IpManagerService
{
    private readonly IConfigurationRoot _configuration;
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly ImperialPluginsClient _imperialPluginsClient;
    private readonly LoggingService _loggingService;
    public IPUser[] UsersCache;
    public Server[] ServersCache;
    public IPPlugin[] PluginsCache;
    public DateTime LastRefresh;
    
    public IpManagerService(DiscordSocketClient discordSocketClient, IConfigurationRoot configuration, ImperialPluginsClient imperialPluginsClient, LoggingService loggingService)
    {
        _configuration = configuration;
        _discordSocketClient = discordSocketClient; 
        _imperialPluginsClient = imperialPluginsClient;
        _loggingService = loggingService;
        
        _discordSocketClient.ButtonExecuted += OnButtonExecuted;
        
        LoginAsync();
    }

    private async Task LoginAsync()
    {
        if (_imperialPluginsClient.IsLoggedIn)
        {
            await _loggingService.LogVerbose(ELogType.Info, "ImperialPlugins client already logged in.");
            return;
        }

        if (bool.TryParse(_configuration["imperial:use_api_key"], out var useApiKey) && bool.TryParse(_configuration["imperial:use_har"], out var useHar))
        {
            if (useApiKey && useHar)
            {
                await _loggingService.LogVerbose(ELogType.Warning, "Both API key and HAR file are enabled. Please disable one.");
                return;
            }
            
            if (useApiKey) {
                if (!_imperialPluginsClient.Login(new IPSessionCredentials(_configuration["imperial:api_key"])))
                {
                    await _loggingService.LogVerbose(ELogType.Error, "Could not log into ImperialPlugins using the provided API key.");
                    return;
                }

                await _loggingService.LogVerbose(ELogType.Info, $"Logged into ImperialPlugins with API as {_imperialPluginsClient.Session.UserName}");
            }
            
            if (useHar)
            {
                if (!_imperialPluginsClient.Login(new IPSessionCredentials("-h", _configuration["imperial:har_path"])))
                {
                    await _loggingService.LogVerbose(ELogType.Error, "Could not log into ImperialPlugins using the provided HAR file.");
                    return;
                }

                await _loggingService.LogVerbose(ELogType.Info, $"Logged into ImperialPlugins with HAR as {_imperialPluginsClient.Session.UserName}");
            }
        }
        
        RefreshCache();
        
        var timer = new Timer();
        timer.Elapsed += timerElapsed;
        timer.Interval = 300000;
        timer.Start();
        await _loggingService.LogVerbose(ELogType.Info, $"Started the cache refresh timer. Interval: {timer.Interval/1000/60}m.");
    }

    private async Task OnButtonExecuted(SocketMessageComponent component)
    {
        var interaction = (IComponentInteraction) component;
        var embed = interaction.Message.Embeds.FirstOrDefault();
        var ticket = _imperialPluginsClient.GetTicketByID(int.Parse(embed?.Footer.Value.Text));
        
        switch (component.Data.CustomId)
        {
            case "whitelist_accept":
                break;
            
            case "whitelist_decline":
                break;
            
            case "whitelist_servers":
                break;
            
            case "ticket_reply":
                var modalTicket = new ModalBuilder()
                    .WithTitle(embed?.Title) // type
                    .WithCustomId("ticket_reply")
                    .AddTextInput("Response", "ticket_reply_response", TextInputStyle.Paragraph,
                        embed.Fields.FirstOrDefault().Value)
                    .Build();

                await component.RespondWithModalAsync(modalTicket);
                break;
            
            case "ticket_close":
                break;
        }
    }

    private void timerElapsed(object sender, EventArgs e)
    {
        RefreshCache();
        
        /*
        var unreadNotifs = _imperialPluginsClient.GetNotifications(100000).Items.Where(x => x.readTime == null);
        foreach (var notification in unreadNotifs)
        {
            var embed = new EmbedBuilder()
                .WithTitle(notification.NotificationType.ToString())
                .WithDescription(notification.Title)
                .WithTimestamp(new DateTimeOffset(notification.creationTime))
                .WithFooter(new EmbedFooterBuilder {IconUrl = notification.ThumbnailUrl, Text = notification.ID})
                .WithUrl(notification.Url)
                .WithColor(Color.Blue)
                .AddField("Message", notification.HtmlContent)
                .Build();

            
            switch (notification.NotificationType)
            {
                case ENotificationType.WhitelistRequest:
                    var componentWhitelist = new ComponentBuilder()
                        .WithButton("Accept", "whitelist_accept", ButtonStyle.Success)
                        .WithButton("Decline", "whitelist_decline", ButtonStyle.Danger)
                        .WithButton("View Servers", "whitelist_servers", ButtonStyle.Secondary)
                        .Build();
                    _discordSocketClient.GetUser(0).SendMessageAsync(embed: embed, components: componentWhitelist);
                    break;
                
                case ENotificationType.Ticket:
                    var componentTicket = new ComponentBuilder()
                        .WithButton("Reply", "ticket_reply", ButtonStyle.Success)
                        .WithButton("Close", "ticket_close", ButtonStyle.Danger)
                        .Build();
                    _discordSocketClient.GetUser(0).SendMessageAsync(embed: embed, components: componentTicket);
                    break;
                default:
                    _discordSocketClient.GetUser(0).SendMessageAsync(embed: embed);
                    break;
            }
        }
        */
    }

    private void RefreshCache()
    {
        if (!_imperialPluginsClient.IsLoggedIn)
        {
            _loggingService.LogVerbose(ELogType.Warning, $"Could not update cache, not logged in. Last time cache was updated: {LastRefresh}");
            return;
        }
        
        _loggingService.LogVerbose(ELogType.Info, "Refreshing cache...");
        
        var timer = new Timer();
        timer.Start();
        
        /*
        UsersCache = _imperialPluginsClient.GetUsers(100000).Items;
        ServersCache = _imperialPluginsClient.GetCustomerServers().Items;
        PluginsCache = _imperialPluginsClient.GetOwnPlugins(10000).Items;
        */
        
        timer.Stop();
        
        LastRefresh = DateTime.UtcNow;
        _loggingService.LogVerbose(ELogType.Info, $"Refreshed cache.");
    }
}