using System.Text.Json;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;

namespace Tgs.Promotion.ReliefMoney.service.MessageReply;

public class ReliefMoneyMonthlyRuleReplyProcessor : IMessageProcessor
{
	private readonly ILogger<ReliefMoneyMonthlyRuleReplyProcessor> _logger;

	public ReliefMoneyMonthlyRuleReplyProcessor(ILogger<ReliefMoneyMonthlyRuleReplyProcessor> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = JsonSerializer.Deserialize<ReliefMoneyMonthlySettings>(
			request.Settings,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
			?? throw new ArgumentNullException(nameof(data));

		_logger.LogInformation("ReliefMoneyMonthly setting : {settings}", settings);

		var response = new CampaignDetailResponse
		{
			CampaignDetail = JsonSerializer.Serialize(settings,
				new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					WriteIndented = true
				}),
			BonusAmountLimit = settings.BonusAmountLimit,
			Percentage = settings.Percentage,
			TurnoverMultiple = settings.TurnoverMultiple
		};

		return ValueTask.FromResult((ReadOnlyMemory<byte>)response.ToByteArray());
	}
}
