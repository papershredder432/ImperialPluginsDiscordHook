using Discord;
using Discord.Interactions;
using ImperialPlugins;
using ImperialPluginsDiscordHook.Services;

namespace ImperialPluginsDiscordHook.Modules;

[Group("customer", "Things for the average user.")]
public class MCustomer : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LoggingService _loggingService;
    private readonly IpManagerService _ipManagerService;
    private readonly InteractionService _interactionService;
    private readonly ImperialPluginsClient _imperialPluginsClient;

    public MCustomer(LoggingService loggingService, IpManagerService ipManagerService, InteractionService interactionService, ImperialPluginsClient imperialPluginsClient)
    {
        _loggingService = loggingService;
        _ipManagerService = ipManagerService;
        _interactionService = interactionService;
        _imperialPluginsClient = imperialPluginsClient;
    }

    [SlashCommand("register", "<email> | Registers you as a customer.")]
    public async Task Register(string email)
    {
        var user = _ipManagerService.GetUserAsync(email);
        if (user == null)
        {
            await RespondAsync("User not found.", ephemeral: true);
            return;
        }

        var prod = _ipManagerService.GetCustomerProducts(user);
        if (prod == null)
        {
            await RespondAsync("No products found.", ephemeral: true);
            return;
        }

        await RespondAsync($"Found {prod.Count} orders. Assigning customer's role(s).", ephemeral: true);
    }
    
    [SlashCommand("ongoingpromotions", "Shows all ongoing promotions.")]
    public async Task OngoingPromotions()
    {
        var promotions = (await _imperialPluginsClient.GetCouponsAsync(100000)).Items.Where(x => x.IsActive && x.IsEnabled && x.Usages < x.MaxUsages /*&& !_couponBlacklist.Contains(x.Name)*/).ToList();
        
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
    
}