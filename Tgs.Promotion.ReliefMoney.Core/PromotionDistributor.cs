using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;
using Tgs.Services.Relief.Protos;

namespace Tgs.Promotion.ReliefMoney.Core;

internal class PromotionDistributor
{
    private readonly DateTimeOffset _endDate;
    private readonly IMessageSender _messageSender;
    private readonly PromotionMainInfo _promotionMainInfo;
    private readonly IPromotionServiceClient _promotionServiceClient;
    private readonly ISerialNumberGenerator _serialNumberGenerator;
    private readonly HashSet<string> _joinPlayers;

    private readonly DateTimeOffset _startDate;
    private readonly decimal _usedAmount;

    public PromotionDistributor(
        PromotionMainInfo promotionMainInfo,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        decimal usedAmount,
        IEnumerable<string> joinPlayers,
        ISerialNumberGenerator serialNumberGenerator,
        IMessageSender messageSender,
        IPromotionServiceClient promotionServiceClient)
    {
        _promotionMainInfo = promotionMainInfo ?? throw new ArgumentNullException(nameof(promotionMainInfo));
        _startDate = startDate;
        _endDate = endDate;
        _usedAmount = usedAmount;
        _joinPlayers = joinPlayers.ToHashSet();
        _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
        _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
    }

    public async Task ProcessDistributesAsync(
        ITgsRepository tgsRepository,
        CancellationToken cancellationToken = default)
    {
        var usedAmount = _usedAmount;

        var firstDate = new DateOnly(_startDate.Year, _startDate.Month, _startDate.Day);

        var distributed = await tgsRepository.CheckHaveReliefDistributeAsync(_promotionMainInfo.Id, firstDate,
            cancellationToken);

        var distributes =
            distributed
                ? await tgsRepository.GetReliefUnDistributedAsync(_promotionMainInfo.Id, firstDate,
                    cancellationToken)
                : await tgsRepository.QueryPromotionReliefSettlementsAsync(
                        _promotionMainInfo.Id,
                        _startDate.UtcDateTime,
                        _endDate.AddDays(1).UtcDateTime,
                        cancellationToken)
                    .GroupBy(settle => new { settle.PlayerCode, settle.Settings.DefaultJoin })
                    .SelectAwait(async g => new
                    {
                        DistributeFrom = _startDate,
                        DistributeTo = _endDate,
                        g.Key.PlayerCode,
                        g.Key.DefaultJoin,
                        _promotionMainInfo.BrandId,
                        PromotionId = _promotionMainInfo.Id,
                        Bonus = await g.SumAsync(g => g.Bonus, cancellationToken).ConfigureAwait(false),
                        TotalWinLoss = await g.SumAsync(
                            s => s.TotalWinLoss, cancellationToken).ConfigureAwait(false),
                        (await g
                            .FirstAsync(cancellationToken)
                            .ConfigureAwait(false)).Settings
                    })
                    .Where(x => x.DefaultJoin || _joinPlayers.Contains(x.PlayerCode))
                    .Where(sum => sum.TotalWinLoss >= sum.Settings.TurnoverThreshold)
                    .Select(sum => new
                    {
                        sum.DistributeFrom,
                        sum.DistributeTo,
                        sum.PlayerCode,
                        sum.BrandId,
                        sum.PromotionId,
                        sum.TotalWinLoss,
                        sum.Settings,
                        CalculateBonus = sum.Bonus
                    })
                    .SelectAwait(async sum =>
                    {
                        var bonusAmount = sum.CalculateBonus > sum.Settings.BonusAmountLimit
                            ? sum.Settings.BonusAmountLimit
                            : sum.CalculateBonus;

                        if (_promotionMainInfo.PromotionLimitAmount.HasValue
                            && usedAmount > _promotionMainInfo.PromotionLimitAmount.Value)
                        {
                            return new PromotionReliefDistribute(
                                await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false),
                                firstDate,
                                sum.PlayerCode,
                                sum.BrandId,
                                _promotionMainInfo.Id,
                                0,
                                0,
                                sum.Settings);
                        }

                        usedAmount += bonusAmount;

                        return new PromotionReliefDistribute(
                            await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false),
                            firstDate,
                            sum.PlayerCode,
                            sum.BrandId,
                            _promotionMainInfo.Id,
                            bonusAmount,
                            bonusAmount * sum.Settings.TurnoverMultiple,
                            sum.Settings);
                    })
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

        if (distributes.Length > 0 && !distributed)
        {
            await tgsRepository.SavePromotionReliefDistributesAsync(
                distributes,
                cancellationToken).ConfigureAwait(false);

            await _promotionServiceClient.UpdatePromotionUsedAmountAsync(_promotionMainInfo.Id, usedAmount,
                distributes.Max(x => x.Id), cancellationToken);
        }

        var conditionHash = new DateOnly(_startDate.Year, _startDate.Month, _startDate.Day).DayNumber;

        var plan = await _promotionServiceClient.GetPromotionDistributePlanAsync(
            _promotionMainInfo.Id,
            conditionHash,
            cancellationToken).ConfigureAwait(false);

        await foreach (var dist in distributes.ToAsyncEnumerable()
                           .ConfigureAwait(false)
                           .WithCancellation(cancellationToken))
        {
            var request = new BonusTransferRequest
            {
                PlanId = plan.Id,
                DistributeId = dist.Id
            };

            _ = await _messageSender.RequestAsync(
                "tgs.relief.ipc.payout",
                request.ToByteArray(),
                cancellationToken).ConfigureAwait(false);
        }
    }
}