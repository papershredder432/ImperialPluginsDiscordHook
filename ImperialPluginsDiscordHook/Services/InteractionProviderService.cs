using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using ImperialPluginsDiscordHook.Enum;

namespace ImperialPluginsDiscordHook.Services;

public class InteractionProviderService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly LoggingService _loggingService;
    private readonly IServiceProvider _serviceProvider;
    
    public InteractionProviderService(DiscordSocketClient client, InteractionService interactionService, LoggingService loggingService, IServiceProvider serviceProvider)
    {
        _client = client;
        _interactionService = interactionService;
        _loggingService = loggingService;
        _serviceProvider = serviceProvider;
        
        _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        _client.InteractionCreated += OnInteractionAsync;
    }

    private async Task OnInteractionAsync(SocketInteraction arg)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, arg);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        }
        catch (Exception ex)
        {
            await _loggingService.LogVerbose(ELogType.Error, ex.Message);
            await _loggingService.LogVerbose(ELogType.Debug, ex.StackTrace);
        }
    }
}