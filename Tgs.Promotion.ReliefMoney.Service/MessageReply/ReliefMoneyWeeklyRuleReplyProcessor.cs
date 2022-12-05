using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;

namespace Tgs.Promotion.ReliefMoney.service.MessageReply;

public class ReliefMoneyWeeklyRuleReplyProcessor : IMessageProcessor
{
	private readonly ILogger<ReliefMoneyWeeklyRuleReplyProcessor> _logger;

	public ReliefMoneyWeeklyRuleReplyProcessor(ILogger<ReliefMoneyWeeklyRuleReplyProcessor> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<ReliefMoneyWeeklySettings>(
                request.Settings,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                })
            ?? throw new ArgumentNullException(nameof(data));

		_logger.LogInformation("ReliefMoneyWeekly setting : {request.Settings}", request.Settings);

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
