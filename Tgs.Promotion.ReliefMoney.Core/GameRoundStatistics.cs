namespace Tgs.Promotion.ReliefMoney.Core;

public record GameRoundStatistics(
	string PlayerCode,
	int VipLevel,
	Guid BrandId,
	Guid GameProviderId,
	GameType GameType,
	decimal TotalWinLoss,
	DateOnly ReportDate);