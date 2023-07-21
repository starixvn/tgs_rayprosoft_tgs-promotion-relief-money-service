using Tgs.MessageQueue;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Service.MessageQueue;

public class DailySettlementHandler : IMessageHandler
{
    private readonly ICommandService<CalculateDailyReliefCommand> _calculateService;
    private readonly ILogger<DailySettlementHandler> _logger;

    public DailySettlementHandler(
        ICommandService<CalculateDailyReliefCommand> calculateService,
        ILogger<DailySettlementHandler> logger)
    {
        _calculateService = calculateService ?? throw new ArgumentNullException(nameof(calculateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DailySettlementRequest.Parser.ParseFrom(data.Span);

        var command = new CalculateDailyReliefCommand(request.SettleDateTime.ToDateTime());

        _logger.LogInformation("Process daily settle {date}", request.SettleDateTime.ToDateTime());

        return _calculateService.ExecuteAsync(command, cancellationToken);
    }
}
