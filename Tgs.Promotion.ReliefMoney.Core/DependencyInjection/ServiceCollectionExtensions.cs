using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Promotion.ReliefMoney.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionRelief(this IServiceCollection services)
		=> services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
}