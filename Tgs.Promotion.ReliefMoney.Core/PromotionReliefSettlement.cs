namespace Tgs.Promotion.ReliefMoney.Core;

public record PromotionReliefSettlement(
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	decimal TotalWinLoss,
	decimal Bonus,
	decimal BucketAmount,
    ReliefSettings Settings);