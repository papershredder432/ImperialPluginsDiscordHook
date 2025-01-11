using Discord;
using Discord.Interactions;
using ImperialPlugins;
using ImperialPlugins.Models.Coupons;
using ImperialPluginsDiscordHook.Services;

namespace ImperialPluginsDiscordHook.Modules;

[Group("customer", "Things for the average user.")]
public class MCustomer : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LoggingService _loggingService;
    private readonly IpManagerService _ipManagerService;
    private readonly InteractionService _interactionService;
    private readonly ImperialPluginsClient _imperialPluginsClient;

    private List<Coupon> allCoupons = new List<Coupon>
    {
        new Coupon {Name = "IP-TEST", Usages = 0, MaxUsages = 100, IsActive = true, IsEnabled = true, ExpirationTime = DateTime.MaxValue, Key = "THEKEY"},
        new Coupon {Name = "IP-TEST2", Usages = 17, MaxUsages = 100, IsActive = true, IsEnabled = true, ExpirationTime = DateTime.MaxValue, Key = "THEKEY2"},
        new Coupon {Name = "IP-TEST3", Usages = 50, MaxUsages = 100, IsActive = false, IsEnabled = true, ExpirationTime = DateTime.MaxValue, Key = "THEKEY3"}, 
        new Coupon {Name = "IP-TEST4", Usages = 100, MaxUsages = 100, IsActive = true, IsEnabled = true, ExpirationTime = DateTime.MaxValue, Key = "THEKEY4"}
    };
    private List<string> _couponBlacklist = new List<string> { "IP-TEST" };

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
        //var promotions = (await _imperialPluginsClient.GetCouponsAsync(100000)).Items.Where(x => x.IsActive && x.IsEnabled && x.Usages < x.MaxUsages && !_couponBlacklist.Contains(x.Name)).ToList();
        var promotions = allCoupons.Where(x => x.IsActive && x.IsEnabled && x.Usages < x.MaxUsages && !_couponBlacklist.Contains(x.Name)).ToList();
        
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