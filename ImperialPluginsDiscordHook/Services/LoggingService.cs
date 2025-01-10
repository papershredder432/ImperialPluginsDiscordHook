using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ImperialPluginsDiscordHook.Enum;
using Microsoft.Extensions.Configuration;
using Pastel;
using Console = System.Console;
using Color = System.Drawing.Color;

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

        var colorType = message.Severity switch
        {
            LogSeverity.Critical => $"{message.Severity.ToString().Pastel(Color.DarkRed)}",
            LogSeverity.Error => $"{message.Severity.ToString().Pastel(Color.Red)}",
            LogSeverity.Warning => $"{message.Severity.ToString().Pastel(Color.DarkOrange)}",
            LogSeverity.Info => $"{message.Severity.ToString().Pastel(Color.Cyan)}",
            LogSeverity.Verbose or LogSeverity.Debug => $"{message.Severity.ToString().Pastel(Color.Yellow)}",
            _ => $"{message.Severity.ToString().Pastel(Color.Gray)}"
        };
        
        // Sources: Discord | Gateway
        
        File.AppendAllText(_logFile, $"[{DateTime.UtcNow:u}] [{message.Severity}/{message.Source}]: {message.Exception?.ToString() ?? message.Message}" + "\n");

        return Console.Out.WriteLineAsync($"[{DateTime.UtcNow:u}] [{colorType}/{message.Source.Pastel(Color.Blue)}]: {message.Exception?.ToString() ?? message.Message}");
    }

    public Task LogVerbose(ELogType logType, string? message)
    {
        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);
        if (!File.Exists(_debugLogFile))
            File.Create(_debugLogFile).Dispose();
            
        if (message == null && message!.Length < 1) message = "RETARD SPOTTED";

        if (logType == ELogType.Debug && _configuration["dev:debug"] != "true")
            return Task.CompletedTask;

        var colorType = logType switch
        {
            ELogType.Debug => $"{logType.ToString().Pastel(Color.Yellow)}",
            ELogType.Error => $"{logType.ToString().Pastel(Color.Red)}",
            ELogType.Info => $"{logType.ToString().Pastel(Color.Cyan)}",
            ELogType.Warning => $"{logType.ToString().Pastel(Color.DarkOrange)}",
            _ => $"{logType.ToString().Pastel(Color.Gray)}"
        };
        
        File.AppendAllText(_debugLogFile, $"[{DateTime.UtcNow:u}] [{logType}/Verbose]: {message}" + "\n");

        return Console.Out.WriteLineAsync($"[{DateTime.UtcNow:u}] [{colorType}/{"Verbose".Pastel(Color.Yellow)}]: {message}");
    }
}