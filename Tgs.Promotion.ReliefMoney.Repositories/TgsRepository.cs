using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Linq;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
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
		var (from, to) = (reportDate, reportDate.AddDays(1));

		var query = _dbContext.PlayerGameRebateAggregates
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.ReportDateTime >= from.UtcDateTime
				&& x.ReportDateTime < to.UtcDateTime)
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
                x.aggregate.BrandId,
                x.aggregate.PlayerCode,
                x.aggregate.GameType,
                x.aggregate.GameProviderId,
                x.aggregate.VipLevel
            }, (player, list) => new
            {
				BrandId = player.BrandId,
				PlayerCode = player.PlayerCode,
				GameType = player.GameType,
				VipLevel = player.VipLevel!.Value,
				GameProviderId = player.GameProviderId,
                WinLoss = list.Sum(l => l.aggregate.WinLoss)
			});

		return query
			.Select(row => new GameRoundStatistics
            {
                PlayerCode = row.PlayerCode,
                VipLevel = row.VipLevel,
                BrandId = row.BrandId,
                GameProviderId = row.GameProviderId,
                GameType = (GameType)row.GameType,
                TotalWinLoss = row.WinLoss
            }).ToAsyncEnumerable();
	}

    public async IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsFromRoundsAsync(DateTimeOffset reportDate, Guid brandId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (from, to) = (reportDate, reportDate.AddDays(1));

        const string spName = @"
WITH round_data
     AS (SELECT r.player_code,
                r.brand_id,
                r.game_provider_id,
                r.game_id,
                Sum(win_loss) win_loss
         FROM   bet.rounds r
         WHERE  closed_on >= @from
                AND closed_on < @to
                AND r.brand_id = @brandId
                AND reopened_on IS NULL
         GROUP  BY 1,
                   2,
                   3,
                   4)

SELECT r.player_code      PlayerCode,
       r.brand_id         BrandId,
       r.game_provider_id GameProviderId,
       v.level            VipLevel,
       g.type             GameType,
       Sum(r.win_loss)    TotalWinLoss
FROM   round_data r
       JOIN dbo.games g
              ON g.id = r.game_id
       JOIN dbo.players p
              ON p.player_code = r.player_code
       JOIN dbo.vip_levels v
              ON v.id = p.vip_id
       LEFT JOIN dbo.brand_games b
              ON r.brand_id = b.brand_id
                 AND r.game_id = b.game_id
WHERE  ( b IS NULL
          OR b.rebate = true )
GROUP  BY 1,
          2,
          3,
          4,
          5;";

        var conn = _dbContext.Database.GetDbConnection();
        var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

        foreach(var data in await conn.QueryAsync<GameRoundStatistics>(spName,
                          new Dictionary<string, object>
                          {
                              { "brandId", brandId },
                              { "from", from.UtcDateTime },
                              { "to", to.UtcDateTime }
                          }, transaction))
        {
            yield return data;
        }
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
