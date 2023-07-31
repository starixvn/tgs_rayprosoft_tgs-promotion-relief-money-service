using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Core.ApplicationServices;

internal class DistributeMonthlyReliefBonusCommandService : ICommandService<DistributeMonthlyReliefBonusCommand>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly ILogger<DistributeMonthlyReliefBonusCommandService> _logger;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IServiceProvider _serviceProvider;
	private readonly ITgsRepository _tgsRepository;

	public DistributeMonthlyReliefBonusCommandService(IPromotionServiceClient promotionServiceClient,
		ITgsRepository tgsRepository,
		IServiceProvider serviceProvider,
		IBrandServiceClient brandServiceClient,
		ILogger<DistributeMonthlyReliefBonusCommandService> logger)
	{
		_promotionServiceClient =
			promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_brandServiceClient = brandServiceClient;
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(
        DistributeMonthlyReliefBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var hour = command.ScheduleDateTime.Hour;

		var value = await _tgsRepository.GetTimeSpanMinutesAsync("PromotionMonthlyReliefMinutes", cancellationToken);

		if (value == null)
		{
			_logger.LogError(@"Settings key ""PromotionMonthlyReliefMinutes"" is null.");

			return;
		}

		var timeSpanMinutesJson = JsonSerializer.Deserialize<Dictionary<int, int?>>(value);

		if (timeSpanMinutesJson?.TryGetValue(hour, out var timeSpanMinutes) == null || timeSpanMinutes == null)
		{
			_logger.LogInformation("Can't found the timeSpanMinutes value from json. (PromotionMonthlyReliefMinutes)");

			return;
		}

		var scheduleDateTime = new DateTimeOffset(command.ScheduleDateTime)
			.ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value));

        var scheduleDate = new DateTimeOffset(scheduleDateTime.Year, scheduleDateTime.Month, scheduleDateTime.Day, 0, 0,
            0, scheduleDateTime.Offset);
        
        var monthFrom = scheduleDate.AddMonths(-1).GetFirstDayOfMonth();
		var monthTo = scheduleDate.AddMonths(-1).GetLastDayOfMonth();

		var brandIds = await _brandServiceClient
			.GetBrandIdsByTimeSpanMinutesAsync(timeSpanMinutes!.Value, cancellationToken)
			.ToArrayAsync(cancellationToken);

		var promotions = await _promotionServiceClient
			.ListPromotionsByPromotionTypeAsync("ReliefMoneyMonthly", cancellationToken)
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.EventDuring.From >= monthFrom.UtcDateTime && x.EventDuring.From <= monthTo.UtcDateTime ||
				x.EventDuring.To >= monthFrom.UtcDateTime && x.EventDuring.To <= monthTo.UtcDateTime ||
				x.EventDuring.From < monthFrom.UtcDateTime && x.EventDuring.To > monthTo.UtcDateTime
			)
			.SelectAwait(async info =>
			{
				var usedAmount = await _promotionServiceClient
					.GetPromotionUsedAmountAsync(info.Id, cancellationToken)
					.ConfigureAwait(false);

				var startTime =
					new DateTimeOffset(info.EventDuring.From).ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value));
				var endTime =
					new DateTimeOffset(info.EventDuring.To).ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value));

				return (
					MainInfo: info,
					PreviousTriggerDate: monthFrom < startTime ? startTime : monthFrom,
					NextTriggerDate: monthTo > endTime ? endTime : monthTo,
					UsedAmount: usedAmount);
			})
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var distributors = promotions
			.Select(p => 
				ActivatorUtilities.CreateInstance<PromotionDistributor>(
					_serviceProvider,
					p.MainInfo,
					p.PreviousTriggerDate,
					p.NextTriggerDate,
					p.UsedAmount));

		await foreach (var distributor in distributors
			               .ToAsyncEnumerable()
			               .WithCancellation(cancellationToken))
			await distributor.ProcessDistributesAsync(
				_tgsRepository,
				cancellationToken).ConfigureAwait(false);
	}
}
