using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.ReliefMoney.Core;
using Tgs.Promotion.ReliefMoney.Repositories.Entities;
using PromotionReliefDistribute = Tgs.Promotion.ReliefMoney.Core.PromotionReliefDistribute;

namespace Tgs.Promotion.ReliefMoney.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly tgsContext _dbContext;

	public TgsRepository(tgsContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsAsync(DateTimeOffset reportDate, Guid[] brandIds, CancellationToken cancellationToken = default)
	{
		var (From, To) = (reportDate, reportDate.AddDays(1));

		var query = _dbContext.PlayerGameRebateAggregates
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.ReportDateTime >= From.UtcDateTime
				&& x.ReportDateTime < To.UtcDateTime)
			.GroupJoin(_dbContext.BrandGames,
				aggregate => new
				{
					aggregate.BrandId,
					aggregate.GameId
				},
				brandGame => new
				{
					brandGame.BrandId,
					brandGame.GameId
				},
				(aggregate, brandGame) => new
				{
					aggregate,
					brandGame
				})
			.SelectMany(x => x.brandGame.DefaultIfEmpty(),
				(x, brandGame) => new
				{
					x.aggregate,
					brandGame
				})
			.Where(x => x.brandGame == null || x.brandGame.Rebate)
			.GroupBy(x => new
			{
				x.aggregate.ReportDateTime,
				x.aggregate.BrandId,
				x.aggregate.PlayerCode,
				x.aggregate.GameType,
				x.aggregate.GameProviderId,
				x.aggregate.VipLevel
			}, (player, list) => new
			{
				ReportDateTime = player.ReportDateTime!.Value,
				BrandId = player.BrandId,
				PlayerCode = player.PlayerCode,
				GameType = player.GameType,
				VipLevel = player.VipLevel!.Value,
				GameProviderId = player.GameProviderId,
                WinLoss = list.Sum(l => l.aggregate.WinLoss)
			});

		return query
			.Select(row => new GameRoundStatistics(
				row.PlayerCode,
				row.VipLevel,
				row.BrandId,
				row.GameProviderId,
				(GameType)row.GameType,
				row.WinLoss,
				DateOnly.FromDateTime(row.ReportDateTime.Date))).ToAsyncEnumerable();
	}

	public async ValueTask SavePromotionReliefSettlementsAsync(DateTimeOffset reportDate,
		IEnumerable<PromotionReliefSettlement> settlements, CancellationToken cancellationToken = default)
	{
		await _dbContext.PromotionReliefDailyStatistics.AddRangeAsync(
			settlements.Select(s => new PromotionReliefDailyStatistic
			{
				ReportDate = new DateOnly(reportDate.Year, reportDate.Month, reportDate.Day),
				ReportDateTime = reportDate.UtcDateTime,
				PlayerCode = s.PlayerCode,
				Bonus = s.Bonus,
				BrandId = s.BrandId,
				BucketAmount = s.BucketAmount,
				PromotionId = s.PromotionId,
				Settings = JsonSerializer.Serialize(s.Settings, PromotionReliefUtility.SerializerOptions),
                TotalWinLoss = s.TotalWinLoss
			}),
			cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<PromotionReliefDistribute[]> GetReliefUnDistributedAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default)
		=> await _dbContext.PromotionReliefDistributes
			.Where(row => row.PromotionId == promotionId)
			.Where(row => row.DistributeDate == date)
			.Select(row => new PromotionReliefDistribute(
				row.Id,
				row.DistributeDate,
				row.PlayerCode,
				row.BrandId,
				row.PromotionId,
				row.Bonus,
				row.BucketAmount,
				JsonSerializer.Deserialize<ReliefSettings>(row.Settings, PromotionReliefUtility.SerializerOptions)!))
			.ToArrayAsync(cancellationToken);

	public async ValueTask<bool> CheckHaveReliefDistributeAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default)
		=> await _dbContext.PromotionReliefDistributes
			.AnyAsync(x =>
				x.PromotionId == promotionId && x.DistributeDate == date, cancellationToken: cancellationToken);

	public IAsyncEnumerable<PlayerJoinRecord> QueryPromotionJoinPlayersAsync(IEnumerable<Guid> promotions, CancellationToken cancellationToken = default)
	{
		var queryPromotions = promotions.ToArray();

		return _dbContext.PromotionParticipants
			.Where(joinPlayer => queryPromotions.Contains(joinPlayer.PromotionId))
			.Select(joinPlayer => new PlayerJoinRecord(joinPlayer.PromotionId, joinPlayer.PlayerCode!))
			.ToAsyncEnumerable();
	}

	public IAsyncEnumerable<PromotionReliefSettlement> QueryPromotionReliefSettlementsAsync(
		Guid promotionId,
		DateTime startDate,
		DateTime endDate,
		CancellationToken cancellationToken = default)
		=> _dbContext.PromotionReliefDailyStatistics.AsQueryable()
			.Where(prds => prds.PromotionId == promotionId)
			.Where(prds => prds.ReportDateTime >= startDate && prds.ReportDateTime < endDate)
			.Select(prds => new
			{
				prds.ReportDate,
				prds.PlayerCode,
				prds.BrandId,
				prds.PromotionId,
				prds.TotalWinLoss,
				prds.Bonus,
				prds.BucketAmount,
				prds.Settings
			})
			.ToAsyncEnumerable()
			.Select(prds => new PromotionReliefSettlement(
				prds.PlayerCode,
				prds.BrandId,
				prds.PromotionId,
				prds.TotalWinLoss,
				prds.Bonus,
				prds.BucketAmount,
				JsonSerializer.Deserialize<ReliefSettings>(prds.Settings, PromotionReliefUtility.SerializerOptions)!));

	public async ValueTask SavePromotionReliefDistributesAsync(IEnumerable<PromotionReliefDistribute> distributes, CancellationToken cancellationToken = default)
	{
		var currentDate = DateTime.UtcNow;
		await _dbContext.PromotionReliefDistributes.AddRangeAsync(
			distributes.Select(s => new Entities.PromotionReliefDistribute
            {
				Id = s.Id,
				DistributeDate = s.DistributeDate,
				PlayerCode = s.PlayerCode,
				Bonus = s.Bonus,
				BrandId = s.BrandId,
				BucketAmount = s.BucketAmount,
				PromotionId = s.PromotionId,
				Settings = JsonSerializer.Serialize(s.Settings, PromotionReliefUtility.SerializerOptions),
				Status = "Create",
				CreatedOn = currentDate
			}),
			cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask SetPromotionReliefDistributedAsync(Guid distributeId, CancellationToken cancellationToken = default)
	{
		var row = await _dbContext.PromotionReliefDistributes
			.FindAsync(new object?[] { distributeId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (row is null)
			return;

		row.Status = "Distributed";

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<string?> GetTimeSpanMinutesAsync(string key, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Settings
			.Where(x => x.Key == key)
			.Select(x => x.Value)
			.FirstOrDefaultAsync(cancellationToken);

	}

	public ValueTask<PromotionReliefDistribute?> GetReliefDistributeAsync(Guid distributeId, CancellationToken cancellationToken = default)
		=> _dbContext.PromotionReliefDistributes
			.FindAsync(new object?[] { distributeId }, cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.Where(row => row is not null)
			.Select(row => row!)
			.Select(row => new PromotionReliefDistribute(
				row.Id,
				row.DistributeDate,
				row.PlayerCode,
				row.BrandId,
				row.PromotionId,
				row.Bonus,
				row.BucketAmount,
				JsonSerializer.Deserialize<ReliefSettings>(row.Settings, PromotionReliefUtility.SerializerOptions)!))
			.FirstOrDefaultAsync(cancellationToken);
}
