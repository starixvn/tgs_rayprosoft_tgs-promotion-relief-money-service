using Tgs.Promotion.ReliefMoney.Service;

namespace Tgs.Promotion.ReliefMoney.service.MessageReply;
public record ReliefMoneyMonthlySettings(
    GameType? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	decimal TurnoverLimit,
	bool PlayerAutoJoin);
