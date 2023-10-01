using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefMoney.Service.MessageQueue;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.UintTests;

public class PayOutBonusHandlerTests
{
	[Test]
	public async Task 執行將救援金提撥到玩家錢包()
	{
		// Arrange
		var fakePayOutService = Substitute.For<ICommandService<PayOutCommand>>();

		var sut = new PayOutBonusHandler(fakePayOutService, NullLogger<PayOutBonusHandler>.Instance);

		var request = new BonusTransferRequest
		{
			PlanId = Guid.NewGuid(),
			DistributeId = Guid.NewGuid()
		};

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakePayOutService.Received(1)
			.ExecuteAsync(Arg.Is(new PayOutCommand(request.PlanId, request.DistributeId)));
	}
}
