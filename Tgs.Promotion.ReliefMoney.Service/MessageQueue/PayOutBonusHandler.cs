using Tgs.MessageQueue;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Service.MessageQueue;

public class PayOutBonusHandler : IMessageHandler
{
    private readonly ICommandService<PayOutCommand> _payOutService;

    public PayOutBonusHandler(ICommandService<PayOutCommand> payOutService)
    {
        _payOutService = payOutService ?? throw new ArgumentNullException(nameof(payOutService));
    }

    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = BonusTransferRequest.Parser.ParseFrom(data.Span);

        var command = new PayOutCommand(request.PlanId, request.DistributeId);

        return _payOutService.ExecuteAsync(command, cancellationToken);
    }
}
