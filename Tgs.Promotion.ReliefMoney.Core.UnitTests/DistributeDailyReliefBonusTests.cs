using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.ReliefMoney.Core.UnitTests;

public class DistributeDailyReliefBonusTests
{
	[Test]
	public async Task 輸入指定日期_發放每日救援金()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new DistributeDailyReliefBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeDailyReliefBonusCommandService>.Instance);

		var date = DateTimeOffset.Parse("2022-12-8 00:00:00+08");

		var command = new DistributeDailyReliefBonusCommand(date.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate = command.ScheduleDateTime.AddDays(-1);

		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(
			Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(
				Arg.Any<int>())
			.Returns(new[]
			{
				brandId
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.GetPromotionUsedAmountAsync(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>())
			.Returns(0m);

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("ReliefMoneyDaily"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new During(DateTimeOffset.Parse("2022-12-5 16:00:00+00").UtcDateTime,
						DateTimeOffset.Parse("2022-12-10 16:00:00+00").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(startDate.ToDateOnly()))
			.Returns(Array.Empty<PromotionReliefDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(DateTimeOffset.Parse("2022-12-6 16:00:00+00").UtcDateTime),
				 Arg.Is(DateTimeOffset.Parse("2022-12-7 16:00:00+00").UtcDateTime));
	}

	[Test]
	public async Task 輸入指定日期_發放每日救援金_()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new DistributeDailyReliefBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeDailyReliefBonusCommandService>.Instance);

		var date = DateTimeOffset.Parse("2022-12-8 0:00:00-08");

		var command = new DistributeDailyReliefBonusCommand(date.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate = command.ScheduleDateTime.AddDays(-1);

		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420,
				"8":-480
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(
				Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(
				Arg.Any<int>())
			.Returns(new[]
			{
				brandId
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.GetPromotionUsedAmountAsync(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>())
			.Returns(0m);

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("ReliefMoneyDaily"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new During(DateTimeOffset.Parse("2022-12-5 00:00:00-08").UtcDateTime,
						DateTimeOffset.Parse("2022-12-10 00:00:00-08").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(startDate.ToDateOnly()))
			.Returns(Array.Empty<PromotionReliefDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(DateTimeOffset.Parse("2022-12-7 00:00:00-08").UtcDateTime),
				 Arg.Is(DateTimeOffset.Parse("2022-12-8 00:00:00-08").UtcDateTime));
	}
}
