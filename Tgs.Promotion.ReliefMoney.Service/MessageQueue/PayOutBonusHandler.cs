using Tgs.MessageQueue;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Service.MessageQueue;

public class PayOutBonusHandler : IMessageHandler
{
    private readonly ICommandService<PayOutCommand> _payOutService;
    private readonly ILogger<PayOutBonusHandler> _logger;

    public PayOutBonusHandler(ICommandService<PayOutCommand> payOutService, ILogger<PayOutBonusHandler> logger)
    {
        _payOutService = payOutService ?? throw new ArgumentNullException(nameof(payOutService));
        _logger = logger;
    }

    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = BonusTransferRequest.Parser.ParseFrom(data.Span);

        var command = new PayOutCommand(request.PlanId, request.DistributeId);

        _logger.LogInformation("Process pay out {PlanId}, {DistributeId}", request.PlanId, request.DistributeId);

        return _payOutService.ExecuteAsync(command, cancellationToken);
    }
}
