namespace Tgs.Promotion.ReliefMoney.Core;

public record PromotionReliefDistribute(
	Guid Id,
	DateOnly DistributeDate,
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	decimal Bonus,
	decimal BucketAmount,
    ReliefSettings Settings);