using Discord.Interactions;
using ImperialPlugins;

namespace ImperialPluginsDiscordHook.Modules;

[Group("generic", "Things for the average user.")]
public class MCustomer : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ImperialPluginsClient _imperialPluginsClient;
    private readonly InteractionService _interactionService;
    
    //private List<string> _couponBlacklist = new List<string> { "IP-TEST" };

    public MCustomer(ImperialPluginsClient imperialPluginsClient, InteractionService interactionService)
    {
        _imperialPluginsClient = imperialPluginsClient;
        _interactionService = interactionService;
    }

    [SlashCommand("customer", "<email> | Registers you as a customer.")]
    public async Task Customer(string email)
    {
        var user = _imperialPluginsClient.GetUsers(100000).Items.FirstOrDefault(x => x.Email == email);
        
        if (user == null)
        {
            await RespondAsync("User not found.", ephemeral: true);
            return;
        }
    }
    
    /*
    [SlashCommand("ongoingpromotions", "Shows all ongoing promotions.")]
    public async Task OngoingPromotions()
    {
        var promotions = (await _imperialPluginsClient.GetCouponsAsync(100000)).Items.Where(x => x.IsActive && x.IsEnabled && x.Usages < x.MaxUsages && !_couponBlacklist.Contains(x.Name)).ToList();
        
        if (promotions.Count == 0)
        {
            await RespondAsync("No promotions found.", ephemeral: true);
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithTitle("Ongoing Promotions")
            .WithDescription("Here are all the ongoing promotions.")
            .WithColor(Color.Blue);

        foreach (var promotion in promotions)
        {
            embed.AddField($"{promotion.Key} | {promotion.Usages}/{promotion.MaxUsages}", promotion.ExpirationTime.ToString());
        }
        
        await RespondAsync(embed: embed.Build());
    }
    */
}