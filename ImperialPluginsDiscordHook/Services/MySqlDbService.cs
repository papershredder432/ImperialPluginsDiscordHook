using ImperialPluginsDiscordHook.Enum;
using ImperialPluginsDiscordHook.Models;
using Microsoft.Extensions.Configuration;
using ShimmyMySherbet.MySQL.EF.Core;

namespace ImperialPluginsDiscordHook.Services;

public class MySqlDbService
{
    private readonly IConfigurationRoot _configuration;
    private readonly LoggingService _loggingService;
    private MySQLEntityClient _entityClient;
    
    private MySqlDbService(IConfigurationRoot configuration, LoggingService loggingService)
    {
        _configuration = configuration;
        _loggingService = loggingService;

        ConnectAsync();
    }
    
    private async Task<bool> ConnectAsync()
    {
        try
        {
            var ec = new MySQLEntityClient(_configuration["database:host"],
                _configuration["database:username"], _configuration["database:password"],
                _configuration["database:database"], int.Parse(_configuration["database:port"]), true);

            _entityClient = ec;
            
            return ec.ConnectAsync().Result;
        }
        catch (Exception e)
        {
            await _loggingService.LogVerbose(ELogType.Error, e.ToString());
            return false;
        }
    }
    
    public async Task InitializeMerchantAsync(MMerchantSettings merchantSettings)
    {
        if (_entityClient.TableExists(_configuration["database:merchant_suffix"]))
        {
            await _loggingService.LogVerbose(ELogType.Info, $"Table already exists for merchant {merchantSettings.DicordId} / {merchantSettings.ImperialPluginsId}");
            return;
        }

        await _entityClient.CreateTableIfNotExistsAsync<MMerchantSettings>(merchantSettings.DicordId + _configuration["database:merchant_suffix"]);
        await _loggingService.LogVerbose(ELogType.Info, $"Table created for merchant {merchantSettings.DicordId} / {merchantSettings.ImperialPluginsId}");
    }
    
    public async Task<MMerchantSettings> GetMerchantSettingsAsync(ulong discordId)
    {
        if (!_entityClient.TableExists(_configuration["database:merchant_suffix"]))
        {
            await _loggingService.LogVerbose(ELogType.Info, $"Table does not exist for merchant {discordId}");
            return null;
        }

        return await _entityClient.QuerySingleAsync<MMerchantSettings>(discordId + _configuration["database:merchant_suffix"]);
    }
    
    public async Task UpdateMerchantSettingsAsync(ulong discordId, MMerchantSettings merchantSettings)
    {
        if (!_entityClient.TableExists(_configuration["database:merchant_suffix"]))
        {
            await _loggingService.LogVerbose(ELogType.Info, $"Table does not exist for merchant {merchantSettings.DicordId}");
            return;
        }

        await _entityClient.UpdateAsync(merchantSettings.DicordId + _configuration["database:merchant_suffix"], discordId + _configuration["database:merchant_suffix"]);
    }
}