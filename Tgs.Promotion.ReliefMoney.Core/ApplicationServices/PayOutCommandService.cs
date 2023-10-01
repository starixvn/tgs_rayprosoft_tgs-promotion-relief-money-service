using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.Core.ApplicationServices;

internal class PayOutCommandService : ICommandService<PayOutCommand>
{
	private readonly ILogger<PayOutCommandService> _logger;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ITgsRepository _tgsRepository;

	public PayOutCommandService(
		ITgsRepository tgsRepository,
		IPromotionServiceClient promotionServiceClient,
		ILogger<PayOutCommandService> logger)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_promotionServiceClient =
			promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(PayOutCommand command, CancellationToken cancellationToken = default)
	{
		var distribute = await _tgsRepository.GetReliefDistributeAsync(
			command.DistributeId,
			cancellationToken).ConfigureAwait(false);

		if (distribute is null)
		{
			_logger.LogWarning("Can't found distribute({distributeId})", command.DistributeId);
			return;
		}

        try
        {
            _ = await _promotionServiceClient.DistributePromotionBonusAsync(
                command.PlanId,
                distribute.PlayerCode,
                distribute.Bonus,
                distribute.BucketAmount,
                null,
                distribute.Id,
                cancellationToken).ConfigureAwait(false);

            await _tgsRepository.SetPromotionReliefDistributedAsync(
                distribute.Id,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Can't distribute to {player}, {bonus} amount, {bucketAmount} bucket amount, {id}. {message}",
                distribute.PlayerCode,
                distribute.Bonus,
                distribute.BucketAmount,
                distribute.Id,
                e.Message);
        }
    }
}