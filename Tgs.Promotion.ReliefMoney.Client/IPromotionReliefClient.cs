namespace Tgs.Promotion.ReliefMoney.Client;

public interface IPromotionReliefClient
{
	Task SettleAsync(DateOnly reportDate, int utcHour);

	Task SendBonusAsync(DateOnly reportDate, int utcHour);
}