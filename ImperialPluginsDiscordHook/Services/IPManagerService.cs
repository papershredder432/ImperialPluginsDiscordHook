using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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
using Pastel;
using Color = System.Drawing.Color;
using Timer = System.Timers.Timer;

namespace ImperialPluginsDiscordHook.Services;

public class IpManagerService
{
    private readonly IConfigurationRoot _configuration;
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly ImperialPluginsClient _imperialPluginsClient;
    private readonly LoggingService _loggingService;
    public EnumerableResponse<IPUser> UsersCache;
    public EnumerableResponse<IPPlugin> PluginsCache;
    public EnumerableResponse<PluginRegistration> RegistrationsCache;
    public DateTime LastRefresh;
    
    public IpManagerService(DiscordSocketClient discordSocketClient, IConfigurationRoot configuration, ImperialPluginsClient imperialPluginsClient, LoggingService loggingService, EnumerableResponse<IPUser> usersCache, EnumerableResponse<IPPlugin> pluginsCache, EnumerableResponse<PluginRegistration> registrationsCache)
    {
        _configuration = configuration;
        _discordSocketClient = discordSocketClient; 
        _imperialPluginsClient = imperialPluginsClient;
        _loggingService = loggingService;
        UsersCache = usersCache;
        PluginsCache = pluginsCache;
        RegistrationsCache = registrationsCache;

        _discordSocketClient.ButtonExecuted += OnButtonExecuted;
        _discordSocketClient.ModalSubmitted += OnModalSubmitted;
        
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
                if (!_imperialPluginsClient.CreateLogin().HarLogin(_configuration["imperial:har_path"]))   
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
        timer.Interval = 5*1000*60;
        timer.Start();
        await _loggingService.LogVerbose(ELogType.Info, $"Started the cache refresh timer. Interval: {timer.Interval/1000/60}m.");
    }
    
    private async Task OnModalSubmitted(SocketModal modal)
    {
        switch (modal.Data.CustomId)
        {
            case "ticket_reply":
                var interaction = (IComponentInteraction) modal;
                var embed = interaction.Message.Embeds.FirstOrDefault();

                if (!int.TryParse(embed.Description.TrimStart("Ticket #".ToCharArray()), out int ticketId))
                {
                    await modal.RespondAsync("Could not parse ticket number.", ephemeral: true);
                    return;   
                }
                
                await modal.RespondAsync("Replied to ticket.", ephemeral: true);
                break;
        }
    }

    private async Task OnButtonExecuted(SocketMessageComponent component)
    {
        var interaction = (IComponentInteraction) component;
        var embed = interaction.Message.Embeds.FirstOrDefault();

        if (!int.TryParse(embed.Description.TrimStart("Ticket #".ToCharArray()), out int ticketId))
        {
            await component.RespondAsync("Could not parse ticket number.", ephemeral: true);
            return;   
        }
        
        switch (component.Data.CustomId)
        {
            case "whitelist_accept":
                break;
            
            case "whitelist_decline":
                break;
            
            case "whitelist_servers":
                GetCustomerServers(GetUserAsync(Regex.Replace(embed.Fields.FirstOrDefault().Value, "<p>[A-Za-z]+ has created a new ticket: Whitelist request</p>", "")));
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
        
        var unreadNotifs = _imperialPluginsClient.GetNotifications(100000).Items/*.Where(x => x.readTime == null)*/
           .Where(x => x.NotificationType is ENotificationType.WhitelistRequest or ENotificationType.Ticket);
        
        foreach (var notification in unreadNotifs)
        {
            var embed = new EmbedBuilder()
                .WithTitle(notification.NotificationType.ToString())
                .WithDescription(notification.Title)
                .WithTimestamp(new DateTimeOffset(notification.creationTime))
                .WithUrl(notification.Url)
                .WithColor(Discord.Color.Blue)
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
                    _discordSocketClient.GetUser(76063689064583168).SendMessageAsync(embed: embed, components: componentWhitelist);
                    break;
                
                case ENotificationType.Ticket:
                    var componentTicket = new ComponentBuilder()
                        .WithButton("Reply", "ticket_reply", ButtonStyle.Success)
                        .WithButton("Close", "ticket_close", ButtonStyle.Danger)
                        .Build();
                    _discordSocketClient.GetUser(76063689064583168).SendMessageAsync(embed: embed, components: componentTicket);
                    break;
                default:
                    _discordSocketClient.GetUser(76063689064583168).SendMessageAsync(embed: embed);
                    break;
            }
        }
    }

    private void RefreshCache()
    {
        if (!_imperialPluginsClient.IsLoggedIn)
        {
            _loggingService.LogVerbose(ELogType.Warning, $"Could not update cache, not logged in. Last time cache was updated: {LastRefresh}");
            return;
        }
        
        _loggingService.LogVerbose(ELogType.Info, "Refreshing cache...");
        
        ThreadPool.QueueUserWorkItem(async(_) =>
        {
            try
            {
                if (UsersCache == null)
                    UsersCache = new EnumerableResponse<IPUser>();
                
                UsersCache = _imperialPluginsClient.GetUsers(100000);
                
                await _loggingService.LogVerbose(ELogType.Info, $"Updated Users cache with {UsersCache.Items.Length} items.");
            } catch (Exception e)
            {
                await _loggingService.LogVerbose(ELogType.Error, $"Error while refreshing cache for Users: {e.Message}");
            }
            
            try
            {
                if (PluginsCache == null)
                    PluginsCache = new EnumerableResponse<IPPlugin>();
                
                PluginsCache = _imperialPluginsClient.GetPlugins(10000);
                
                await _loggingService.LogVerbose(ELogType.Info, $"Updated Plugins cache with {PluginsCache.Items.Length} items.");
            } catch (Exception e)
            {
                await _loggingService.LogVerbose(ELogType.Error, $"Error while refreshing cache for Plugins: {e.Message}");
            }
            
            try
            {
                if (RegistrationsCache == null)
                    RegistrationsCache = new EnumerableResponse<PluginRegistration>();
                
                RegistrationsCache = _imperialPluginsClient.GetRegistrations(10000);
                
                await _loggingService.LogVerbose(ELogType.Info, $"Updated Registrations cache with {RegistrationsCache.Items.Length} items.");
            } catch (Exception e)
            {
                await _loggingService.LogVerbose(ELogType.Error, $"Error while refreshing cache for Registrations: {e.Message}");
            }
        });
        
        LastRefresh = DateTime.UtcNow;
        _loggingService.LogVerbose(ELogType.Info, $"Refreshed cache.");
    }
    
    public IPUser? GetUserAsync(string user) =>
        UsersCache == null ? null : UsersCache.Items.FirstOrDefault(x => x.Id == user || x.Email.Equals(user, StringComparison.InvariantCultureIgnoreCase) || x.UserName.Equals(user, StringComparison.InvariantCultureIgnoreCase));
    
    public List<PluginRegistration>? GetCustomerProducts(IPUser user) =>
        RegistrationsCache == null ? null : RegistrationsCache.Items.Where(i => i.OwnerName.Contains(user.UserName, StringComparison.InvariantCultureIgnoreCase)).ToList();

    public List<ProductInstallation> GetCustomerServers(IPUser user) => 
        _imperialPluginsClient.GetInstallations(user.Id, 10000).Items.ToList();
}