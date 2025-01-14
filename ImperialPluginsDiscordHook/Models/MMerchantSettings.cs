namespace ImperialPluginsDiscordHook.Models;

public class MMerchantSettings
{
    public MMerchantSettings()
    {
        
    }

    public ulong DicordId { get; set; }
    public string ImperialPluginsId { get; set; } = "not_null_lol";

    public bool PromotionsListEnabled { get; set; } = false;
}