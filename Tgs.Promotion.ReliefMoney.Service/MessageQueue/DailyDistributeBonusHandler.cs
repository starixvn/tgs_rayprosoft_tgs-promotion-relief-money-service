using Tgs.MessageQueue;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Service.MessageQueue;

public class DailyDistributeBonusHandler : IMessageHandler
{
    private readonly ICommandService<DistributeDailyReliefBonusCommand> _distributeService;
    private readonly ICommandService<DistributeWeeklyReliefBonusCommand> _weeklyDistributeService;
    private readonly ICommandService<DistributeMonthlyReliefBonusCommand> _monthlyDistributeService;
    private readonly ILogger<DailyDistributeBonusHandler> _logger;

    public DailyDistributeBonusHandler(
        ICommandService<DistributeDailyReliefBonusCommand> distributeService,
        ICommandService<DistributeWeeklyReliefBonusCommand> weeklyDistributeService,
        ICommandService<DistributeMonthlyReliefBonusCommand> monthlyDistributeService,
        ILogger<DailyDistributeBonusHandler> logger)
    {
        _distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
        _weeklyDistributeService = weeklyDistributeService ?? throw new ArgumentNullException(nameof(weeklyDistributeService));
        _monthlyDistributeService = monthlyDistributeService ?? throw new ArgumentNullException(nameof(monthlyDistributeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = ReliefDistributeRequest.Parser.ParseFrom(data.Span);

        _logger.LogInformation("Process daily settle {date}", request.DistributeDateTime.ToDateTime());

        await _distributeService.ExecuteAsync(
            new DistributeDailyReliefBonusCommand(request.DistributeDateTime.ToDateTime()),
            cancellationToken);

        await _weeklyDistributeService.ExecuteAsync(
            new DistributeWeeklyReliefBonusCommand(request.DistributeDateTime.ToDateTime()),
            cancellationToken);

        await _monthlyDistributeService.ExecuteAsync(
            new DistributeMonthlyReliefBonusCommand(request.DistributeDateTime.ToDateTime()),
            cancellationToken);
    }
}
