namespace Tgs.Promotion.ReliefMoney.service.MessageReply;
public record ReliefMoneyMonthlySettings(
	short? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	decimal TurnoverLimit,
	bool PlayerAutoJoin);
