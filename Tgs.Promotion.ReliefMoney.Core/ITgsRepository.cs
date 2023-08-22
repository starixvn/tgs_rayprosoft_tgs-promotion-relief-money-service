namespace Tgs.Promotion.ReliefMoney.Core;

public interface ITgsRepository
{
	IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsAsync(DateTimeOffset reportDate, Guid[] brandIds,
		CancellationToken cancellationToken = default);

    IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsFromGameRoundAggregateAsync(DateTimeOffset reportDate, Guid brandId,
        CancellationToken cancellationToken = default);

    ValueTask SavePromotionReliefSettlementsAsync(DateTimeOffset reportDate,
		IEnumerable<PromotionReliefSettlement> settlements, CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionReliefSettlement> QueryPromotionReliefSettlementsAsync(
		Guid promotionId,
		DateTime startDate,
		DateTime endDate,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionReliefDistribute?> GetReliefDistributeAsync(Guid distributeId,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionReliefDistribute[]> GetReliefUnDistributedAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default);

	ValueTask<bool> CheckHaveReliefDistributeAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PlayerJoinRecord> QueryPromotionJoinPlayersAsync(IEnumerable<Guid> promotions,
		CancellationToken cancellationToken = default);

	ValueTask SavePromotionReliefDistributesAsync(IEnumerable<PromotionReliefDistribute> distributes,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionReliefDistributedAsync(Guid distributeId, CancellationToken cancellationToken = default);

	ValueTask<string?> GetTimeSpanMinutesAsync(string key, CancellationToken cancellationToken = default);
}