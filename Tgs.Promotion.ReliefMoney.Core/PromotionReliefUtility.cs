using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.ReliefMoney.Core;

public static class PromotionReliefUtility
{
	public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		WriteIndented = false,
		Converters = { new JsonStringEnumConverter() }
	};
}