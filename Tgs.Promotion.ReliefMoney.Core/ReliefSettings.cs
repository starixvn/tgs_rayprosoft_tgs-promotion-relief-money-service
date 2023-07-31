using System.Text.Json.Serialization;

namespace Tgs.Promotion.ReliefMoney.Core;

public record ReliefSettings(
	GameType? GameType,
	IEnumerable<Guid>? GameProviderIds,
	[property: JsonPropertyName("turnoverLimit")]
	decimal TurnoverThreshold,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	[property: JsonPropertyName("playerAutoJoin")]
	bool DefaultJoin,
	IEnumerable<int> VipLevels);