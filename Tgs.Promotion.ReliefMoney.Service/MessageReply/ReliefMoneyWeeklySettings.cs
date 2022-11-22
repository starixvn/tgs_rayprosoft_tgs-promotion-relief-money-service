namespace Tgs.Promotion.ReliefMoney.service.MessageReply;
public record ReliefMoneyWeeklySettings(
	short? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	decimal TurnoverLimit,
	bool PlayerAutoJoin);
