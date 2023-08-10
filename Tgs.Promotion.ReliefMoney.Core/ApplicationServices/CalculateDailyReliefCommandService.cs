using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Core.ApplicationServices;

internal class CalculateDailyReliefCommandService : ICommandService<CalculateDailyReliefCommand>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly ILogger<CalculateDailyReliefCommandService> _logger;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ITgsRepository _tgsRepository;

	public CalculateDailyReliefCommandService(
		ITgsRepository tgsRepository,
		IPromotionServiceClient promotionServiceClient,
		IBrandServiceClient brandServiceClient,
		ILogger<CalculateDailyReliefCommandService> logger)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_promotionServiceClient =
			promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(CalculateDailyReliefCommand command,
		CancellationToken cancellationToken = default)
	{
		var hour = command.SettleDateTime.Hour;

		var value = await _tgsRepository.GetTimeSpanMinutesAsync("PromotionDailySettleMinutes", cancellationToken);

		if (value == null)
		{
			_logger.LogError(@"Settings key ""PromotionDailySettleMinutes"" is null.");

			return;
		}

		var timeSpanMinutesJson = JsonSerializer.Deserialize<Dictionary<int, int?>>(value);

		if (timeSpanMinutesJson?.TryGetValue(hour, out var timeSpanMinutes) == null || timeSpanMinutes == null)
		{
			_logger.LogInformation("Can't found the timeSpanMinutes value from json. (PromotionDailySettleMinutes)");

			return;
		}

		var reportDateTime = new DateTimeOffset(command.SettleDateTime)
			.ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value)).AddDays(-1);

        var reportDate = new DateTimeOffset(reportDateTime.Year, reportDateTime.Month, reportDateTime.Day, 0, 0,
            0, reportDateTime.Offset);

        var brandIds = await _brandServiceClient
			.GetBrandIdsByTimeSpanMinutesAsync(timeSpanMinutes!.Value, cancellationToken)
			.ToArrayAsync(cancellationToken);

        var statistics = await GetGameRoundStatisticsAsync(reportDate, brandIds, cancellationToken)
			.Do(record => _logger.LogDebug("GameRound: {record}", record))
			.ToLookupAsync(s => s.BrandId, cancellationToken)
			.ConfigureAwait(false);

        var promotions = await new[]
            {
                _promotionServiceClient
                    .ListPromotionsByPromotionTypeAsync("ReliefMoneyDaily", cancellationToken),
                _promotionServiceClient
                    .ListPromotionsByPromotionTypeAsync("ReliefMoneyWeekly", cancellationToken),
                _promotionServiceClient
                    .ListPromotionsByPromotionTypeAsync("ReliefMoneyMonthly", cancellationToken)
            }.ToAsyncEnumerable()
            .Merge()
            .Do(info => _logger.LogDebug("Promotion: {info}", info))
            .Where(x => x.EventDuring.From <= reportDate.UtcDateTime && x.EventDuring.To >= reportDate.UtcDateTime)
            .Where(info => statistics.Contains(info.BrandId))
            .Do(info => _logger.LogDebug("Match Promotion: {info}", info))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var playerJoinList = await _tgsRepository
            .QueryPromotionJoinPlayersAsync(promotions.Select(p => p.Id), cancellationToken)
            .ToLookupAsync(j => j.PromotionId, cancellationToken)
            .ConfigureAwait(false);

        var settlements = promotions.ToAsyncEnumerable()
            .Select(info => new PromotionCounter(
                info,
                _promotionServiceClient,
                playerJoinList[info.Id].Select(j => j.PlayerCode)))
            .Do(counter => counter.InitialAsync(cancellationToken))
            .SelectMany(counter => counter.ProcessBatchAsync(statistics[counter.BrandId], cancellationToken));

        await _tgsRepository.SavePromotionReliefSettlementsAsync(reportDate, settlements.ToEnumerable(),
            cancellationToken).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<GameRoundStatistics> GetGameRoundStatisticsAsync(DateTimeOffset reportDate, Guid[] brandIds, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (reportDate.Offset.TotalHours % 1 == 0)
        {
            await foreach (var gameRoundStatistics in _tgsRepository
                         .QueryGameRoundStatisticsAsync(reportDate, brandIds, cancellationToken))
            {
                yield return gameRoundStatistics;
            }
        }
        else
        {
            foreach (var brandId in brandIds)
            {
                await foreach (var gameRoundStatistics in _tgsRepository
                             .QueryGameRoundStatisticsFromRoundsAsync(reportDate, brandId, cancellationToken))
                {
                    yield return gameRoundStatistics;
                }
            }
        }
    }
}