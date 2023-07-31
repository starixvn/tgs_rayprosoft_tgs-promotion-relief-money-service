using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tgs.Promotion.ReliefMoney.Client.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionReliefClient(
		this IServiceCollection services,
		Action<PromotionReliefOptions, IServiceProvider> configure)
		=> services
			.AddOptions<PromotionReliefOptions>()
			.Configure(configure)
			.Services
			.AddHttpClient<IPromotionReliefClient, PromotionReliefClient>((sp, c) =>
			{
				var options = sp.GetRequiredService<IOptions<PromotionReliefOptions>>().Value;

				c.BaseAddress = options.Url;
			}).Services;
}