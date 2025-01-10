using ImperialPlugins;
using ImperialPlugins.Models.Plugins;
using ImperialPlugins.Models.Servers;
using ImperialPluginsDiscordHook.Enum;
using Timer = System.Timers.Timer;

namespace ImperialPluginsDiscordHook.Services;

public class ImperialPluginsCacheService
{
    private readonly ImperialPluginsClient _imperialPluginsClient;
    private readonly LoggingService _loggingService;
    public IPUser[] UsersCache;
    public Server[] ServersCache;
    public IPPlugin[] PluginsCache;
    public DateTime LastRefresh;
    
    public ImperialPluginsCacheService(ImperialPluginsClient imperialPluginsClient, LoggingService loggingService)
    {
        _imperialPluginsClient = imperialPluginsClient;
        _loggingService = loggingService;
        
        RefreshCache();
        
        var timer = new Timer();
        timer.Elapsed += timerElapsed;
        timer.Interval = 300000;
        timer.Start();
    }
    
    private void timerElapsed(object sender, EventArgs e)
    {
        RefreshCache();
    }

    private void RefreshCache()
    {
        if (!_imperialPluginsClient.IsLoggedIn)
        {
            _loggingService.LogVerbose(ELogType.WARNING, $"Could not update cache, not logged in. Last time cache was updated: {LastRefresh}");
            return;
        }
        
        _loggingService.LogVerbose(ELogType.INFO, "Refreshing cache...");

        var timer = new Timer();
        timer.Start();
        
        UsersCache = _imperialPluginsClient.GetUsers(100000).Items;
        ServersCache = _imperialPluginsClient.GetCustomerServers().Items;
        PluginsCache = _imperialPluginsClient.GetOwnPlugins(10000).Items;
        
        timer.Stop();
        
        LastRefresh = DateTime.UtcNow;
        _loggingService.LogVerbose(ELogType.INFO, $"Refreshed cache. Took {timer.Interval}ms.");
    }
}