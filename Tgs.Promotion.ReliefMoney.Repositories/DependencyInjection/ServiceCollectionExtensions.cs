using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.ReliefMoney.Core;
using Tgs.Promotion.ReliefMoney.Repositories.Entities;

namespace Tgs.Promotion.ReliefMoney.Repositories.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionReliefRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbContextConfigure)
		=> services
			.AddDbContext<tgsContext>(dbContextConfigure)
			.AddTransient<ITgsRepository, TgsRepository>();
}
