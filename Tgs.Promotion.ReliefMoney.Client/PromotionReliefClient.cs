using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.ReliefMoney.Client;

internal class PromotionReliefClient : IPromotionReliefClient
{
	private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		Converters = { new JsonStringEnumConverter() }
	};

	private readonly HttpClient _httpClient;

	public PromotionReliefClient(HttpClient httpClient)
		=> _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

	public async Task SettleAsync(DateOnly reportDate, int hour)
	{
		var request = new
		{
			ReportDate = reportDate,
			Hour = hour
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PostAsync(
			$"/schedule/settle/{reportDate.ToString("yyyy-MM-dd")}/{hour}",
			requestContent).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public async Task SendBonusAsync(DateOnly reportDate, int hour)
	{
		var request = new
		{
			ReportDate = reportDate,
			Hour = hour
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PostAsync(
			$"/schedule/bonus/{reportDate.ToString("yyyy-MM-dd")}/{hour}",
			requestContent).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}
}