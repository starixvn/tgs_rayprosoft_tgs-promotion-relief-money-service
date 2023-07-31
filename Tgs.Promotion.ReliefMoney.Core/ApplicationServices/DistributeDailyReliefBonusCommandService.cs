using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Core.ApplicationServices;

internal class DistributeDailyReliefBonusCommandService : ICommandService<DistributeDailyReliefBonusCommand>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly ILogger<DistributeDailyReliefBonusCommandService> _logger;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IServiceProvider _serviceProvider;
	private readonly ITgsRepository _tgsRepository;

	public DistributeDailyReliefBonusCommandService(
		IPromotionServiceClient promotionServiceClient,
		ITgsRepository tgsRepository,
		IServiceProvider serviceProvider,
		IBrandServiceClient brandServiceClient,
		ILogger<DistributeDailyReliefBonusCommandService> logger)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(
		DistributeDailyReliefBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var hour = command.ScheduleDateTime.Hour;

		var value = await _tgsRepository.GetTimeSpanMinutesAsync("PromotionDailyReliefMinutes", cancellationToken);

		if (value == null)
		{
			_logger.LogError(@"Settings key ""PromotionDailyReliefMinutes"" is null.");

			return;
		}

		var timeSpanMinutesJson = JsonSerializer.Deserialize<Dictionary<int, int?>>(value);

		if (timeSpanMinutesJson?.TryGetValue(hour, out var timeSpanMinutes) == null || timeSpanMinutes == null)
		{
			_logger.LogInformation("Can't found the timeSpanMinutes value from json. (PromotionDailyReliefMinutes)");

			return;
		}

		var reportDateTime = new DateTimeOffset(command.ScheduleDateTime)
			.ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value)).AddDays(-1);

        var scheduleDate = new DateTimeOffset(reportDateTime.Year, reportDateTime.Month, reportDateTime.Day, 0, 0,
            0, reportDateTime.Offset);

        var brandIds = await _brandServiceClient
			.GetBrandIdsByTimeSpanMinutesAsync(timeSpanMinutes!.Value, cancellationToken)
			.ToArrayAsync(cancellationToken);

		var promotions = await _promotionServiceClient
			.ListPromotionsByPromotionTypeAsync("ReliefMoneyDaily", cancellationToken)
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.EventDuring.From <= scheduleDate.UtcDateTime &&
				x.EventDuring.To >= scheduleDate.UtcDateTime)
			.SelectAwait(async x => new
			{
				MainInfo = x,
				UsedAmount = await _promotionServiceClient
					.GetPromotionUsedAmountAsync(x.Id, cancellationToken)
					.ConfigureAwait(false)
			})
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var distributors = promotions
			.Select(promotion =>
				ActivatorUtilities.CreateInstance<PromotionDistributor>(
					_serviceProvider,
					promotion.MainInfo,
                    scheduleDate,
                    scheduleDate,
					promotion.UsedAmount));

		await foreach (var distributor in distributors
			               .ToAsyncEnumerable()
			               .WithCancellation(cancellationToken))
			await distributor.ProcessDistributesAsync(
				_tgsRepository,
				cancellationToken).ConfigureAwait(false);
	}
}
