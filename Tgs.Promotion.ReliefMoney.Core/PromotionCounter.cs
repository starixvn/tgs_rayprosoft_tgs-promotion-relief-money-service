using Tgs.Promotion.Clients;

namespace Tgs.Promotion.ReliefMoney.Core;

internal class PromotionCounter
{
    private readonly HashSet<string> _joinPlayers;
    private readonly PromotionMainInfo _promotionMainInfo;
    private readonly IPromotionServiceClient _promotionServiceClient;
    private int _gameProviderIdCount;
    private ReliefSettings _reliefSettings = default!;

    public Guid BrandId => _promotionMainInfo.BrandId;

    public PromotionCounter(
        PromotionMainInfo promotionMainInfo,
        IPromotionServiceClient promotionServiceClient,
        IEnumerable<string> joinPlayers)
    {
        _promotionMainInfo = promotionMainInfo ?? throw new ArgumentNullException(nameof(promotionMainInfo));
        _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
        _joinPlayers = joinPlayers.ToHashSet();
    }

    public async Task InitialAsync(CancellationToken cancellationToken = default)
    {
        var promotionRule = await _promotionServiceClient.GetPromotionRuleAsync<PromotionRule>(
            _promotionMainInfo.Id,
            cancellationToken).ConfigureAwait(false);

        if (promotionRule is null)
        {
            throw new NullReferenceException(nameof(promotionRule));
        }

        _reliefSettings = promotionRule.CampaignDetail with
        {
            VipLevels = promotionRule.VipLevels
        };

        _gameProviderIdCount = _reliefSettings.GameProviderIds?.Count() ?? 0;
    }

    public IAsyncEnumerable<PromotionReliefSettlement> ProcessBatchAsync(
        IEnumerable<GameRoundStatistics> statistics,
        CancellationToken cancellationToken = default)
        => (from gameRound in statistics
            where gameRound.BrandId == BrandId
            where _reliefSettings.DefaultJoin
                || _joinPlayers.Contains(gameRound.PlayerCode)
            where _reliefSettings.VipLevels.Contains(gameRound.VipLevel)
            where _reliefSettings.GameType is null
                || gameRound.GameType == _reliefSettings.GameType
            where _gameProviderIdCount == 0
                || _reliefSettings.GameProviderIds!.Contains(gameRound.GameProviderId)
            group gameRound by gameRound.PlayerCode
            into g
            let totalWinLoss = g.Sum(round => round.TotalWinLoss)
            let calculateBonus = totalWinLoss * _reliefSettings.Percentage / 100M
            let bonus = calculateBonus
            select new PromotionReliefSettlement(
                g.Key,
                BrandId,
                _promotionMainInfo.Id,
                totalWinLoss,
                bonus,
                _reliefSettings.TurnoverMultiple * bonus,
                _reliefSettings))
            .ToAsyncEnumerable()
            .Do(_ => cancellationToken.ThrowIfCancellationRequested());

    internal record PromotionRule(
        ReliefSettings CampaignDetail,
        IEnumerable<int> VipLevels);
}