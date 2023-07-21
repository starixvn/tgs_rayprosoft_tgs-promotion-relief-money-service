using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.Promotion.ReliefMoney.Service.MessageQueue;
using Tgs.Promotion.ReliefService.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ReliefMoney.UintTests;

public class DailySettlementHandlerTests
{
	[Test]
	public async Task 處理排程觸發結算每日救援金統計()
	{
		// Arrange
		var fakeCalculateService = Substitute.For<ICommandService<CalculateDailyReliefCommand>>();

		var sut = new DailySettlementHandler(
			fakeCalculateService,
			NullLogger<DailySettlementHandler>.Instance);

		var date = DateTimeOffset.Parse("2022-12-8 16:00:00+00");

		var request = new DailySettlementRequest
		{
			SettleDateTime = Timestamp.FromDateTime(date.UtcDateTime)
		};

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakeCalculateService.Received(1)
			.ExecuteAsync(Arg.Is(new CalculateDailyReliefCommand(request.SettleDateTime.ToDateTime())));
	}
}
