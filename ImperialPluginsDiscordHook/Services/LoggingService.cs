using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ImperialPluginsDiscordHook.Enum;
using Microsoft.Extensions.Configuration;
using Console = System.Console;

namespace ImperialPluginsDiscordHook.Services;

public class LoggingService
{
    private readonly IConfigurationRoot _configuration;
    
    private string _logDirectory { get; }
    private string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyy-M-d}.txt");
    private string _debugLogFile => Path.Combine(_logDirectory, $"debug_{DateTime.UtcNow:yyyy-M-d}.txt");
    
    public LoggingService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot configuration)
    {
        _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        discord.Log += OnLogAsync;
        commands.Log += OnLogAsync;
        
        _configuration = configuration;
    }

    private Task OnLogAsync(LogMessage message)
    {
        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);
        if (!File.Exists(_logFile))
            File.Create(_logFile).Dispose();

        string logText = $"[{DateTime.UtcNow:u}] [{message.Severity}/{message.Source}]: {message.Exception?.ToString() ?? message.Message}";
        File.AppendAllText(_logFile, logText + "\n");

        return Console.Out.WriteLineAsync(logText);
    }

    public Task LogVerbose(ELogType logType, string? message)
    {
        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);
        if (!File.Exists(_debugLogFile))
            File.Create(_debugLogFile).Dispose();
            
        if (message == null && message!.Length < 1) message = "RETARD SPOTTED";

        if (logType == ELogType.DEBUG && _configuration["dev:debug"] != "true")
            return Task.CompletedTask;

        var logText = $"[{DateTime.UtcNow:u}] [{logType}]: {message}";
        File.AppendAllText(_debugLogFile, logText + "\n");

        return Console.Out.WriteLineAsync(logText);
    }
}