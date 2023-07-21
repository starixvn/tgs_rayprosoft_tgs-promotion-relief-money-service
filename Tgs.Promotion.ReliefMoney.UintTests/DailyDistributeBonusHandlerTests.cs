using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefMoney.Service.MessageQueue;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.UintTests;

public class DailyDistributeBonusHandlerTests
{
	[Test]
	public async Task 處理排程每日救援金發放()
	{
		// Arrange
		var fakeDistributeService = Substitute.For<ICommandService<DistributeDailyReliefBonusCommand>>();
		var fakeWeeklyDistributeService = Substitute.For<ICommandService<DistributeWeeklyReliefBonusCommand>>();

		var sut = new DailyDistributeBonusHandler(
			fakeDistributeService,
			fakeWeeklyDistributeService,
			NullLogger<DailyDistributeBonusHandler>.Instance);

		var date = DateTimeOffset.Parse("2022-12-8 16:00:00+00");

		var request = new ReliefDistributeRequest
		{
			DistributeDateTime = Timestamp.FromDateTime(date.UtcDateTime)
		};

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakeDistributeService.Received(1)
			.ExecuteAsync(Arg.Is(new DistributeDailyReliefBonusCommand(request.DistributeDateTime.ToDateTime())));
		_ = fakeWeeklyDistributeService.Received(1)
			.ExecuteAsync(Arg.Is(new DistributeWeeklyReliefBonusCommand(request.DistributeDateTime.ToDateTime())));
	}
}
