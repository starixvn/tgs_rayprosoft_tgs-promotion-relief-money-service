using Tgs.MessageQueue;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Service.MessageQueue;

public class SupplementDailySettlementHandler : IMessageHandler
{
    private readonly ICommandService<SupplementCalculateDailyReliefCommand> _calculateService;
    private readonly ILogger<SupplementDailySettlementHandler> _logger;

    public SupplementDailySettlementHandler(
        ICommandService<SupplementCalculateDailyReliefCommand> calculateService,
        ILogger<SupplementDailySettlementHandler> logger)
    {
        _calculateService = calculateService ?? throw new ArgumentNullException(nameof(calculateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = SupplementDailySettlementRequest.Parser.ParseFrom(data.Span);

        var command =
            new SupplementCalculateDailyReliefCommand(request.SettleDateTime.ToDateTime(), request.PromotionType);

        _logger.LogInformation("Process supplement daily settle {date}", request.SettleDateTime.ToDateTime());

        return _calculateService.ExecuteAsync(command, cancellationToken);
    }
}
