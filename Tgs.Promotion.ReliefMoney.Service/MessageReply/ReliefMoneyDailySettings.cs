using Tgs.Promotion.ReliefMoney.Service;

namespace Tgs.Promotion.ReliefMoney.service.MessageReply;
public record ReliefMoneyDailySettings(
    GameType? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	decimal TurnoverLimit,
	bool PlayerAutoJoin);
