using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.ReliefMoney.Core.UnitTests;

public class DistributeMonthlyReliefBonusTests
{
	[Test]
	public async Task 每月救援金發放_觸發時間未到不會發放()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2023-01-01 23:00:00-08");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();

		var timeSpanMinutes = """
		{
			"16": 480,
			"1": 420,
			"7": -480
		}
		""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>()).Returns(new[]
		{
			brandId
		}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-01-01 00:00:00+07").UtcDateTime,
						DateTimeOffset.Parse("2023-01-31 00:00:00+07").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(0)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
	}

	[Test]
	public async Task 每月救援金發放_觸發時間未到不會發放_()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2022-12-26 00:00:00-08");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();

		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420,
				"8": -480
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(
			Arg.Any<int>())
			.Returns(new[]
			{
				brandId
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-01-01 00:00:00-08").UtcDateTime,
						DateTimeOffset.Parse("2023-01-31 00:00:00-08").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(0)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Any<DateTime>(),
				Arg.Any<DateTime>());
	}

	[Test]
	public async Task 輸入日期_發放每月救援金_With_8_Of_Time_Zone_For_First_Day()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2023-07-01 00:00:00+08");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate =
			new DateTimeOffset(DateOnly.Parse("2023-06-01").ToDateTime(TimeOnly.MinValue),
				TimeSpan.FromMinutes(480));
		var firstDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(
				Arg.Any<string>())
			.Returns(timeSpanMinutes);

        _ = fakeTgsRepository.CheckHaveReliefDistributeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
            .Returns(false);

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
				Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-01-31 16:00:00+00").UtcDateTime,
						DateTimeOffset.Parse("2023-06-29 16:00:00+00").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(firstDate))
			.Returns(Array.Empty<PromotionReliefDistribute>());

        // Act
        await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Is(DateTimeOffset.Parse("2023-06-1 00:00:00+08").UtcDateTime),
				Arg.Is(DateTimeOffset.Parse("2023-07-01 00:00:00+08").UtcDateTime));
	}

	[Test]
	public async Task 輸入日期_發放每月救援金_With_8_Of_Time_Zone_For_Last_Day()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2023-07-31 13:00:00+00");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate =
			new DateTimeOffset(DateOnly.Parse("2023-06-01").ToDateTime(TimeOnly.MinValue),
				TimeSpan.FromMinutes(480));
		var firstDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
		var timeSpanMinutes = """
			{
				"13": 480,
				"17": 420
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(
				Arg.Any<string>())
			.Returns(timeSpanMinutes);

        _ = fakeTgsRepository.CheckHaveReliefDistributeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
            .Returns(false);

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
				Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-06-01 00:00:00+08").UtcDateTime,
						DateTimeOffset.Parse("2023-06-30 00:00:00+08").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(firstDate))
			.Returns(Array.Empty<PromotionReliefDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Is(DateTimeOffset.Parse("2023-06-01 00:00:00+08").UtcDateTime),
				Arg.Is(DateTimeOffset.Parse("2023-07-01 00:00:00+08").UtcDateTime));
	}

	[Test]
	public async Task 輸入日期_發放每月救援金_With_8_Of_Time_Zone_For_Middle_Day()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2023-07-15 13:00:00+00");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate =
			new DateTimeOffset(DateOnly.Parse("2023-06-01").ToDateTime(TimeOnly.MinValue),
				TimeSpan.FromMinutes(480));
		var firstDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
		var timeSpanMinutes = """
			{
				"13": 480,
				"17": 420
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(
				Arg.Any<string>())
			.Returns(timeSpanMinutes);

        _ = fakeTgsRepository.CheckHaveReliefDistributeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
            .Returns(false);

        _ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(
			Arg.Any<int>())
			.Returns(new[]
			{
				brandId
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-06-01 00:00:00+08").UtcDateTime,
						DateTimeOffset.Parse("2023-06-30 00:00:00+08").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.GetPromotionUsedAmountAsync(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>())
			.Returns(0m);

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(firstDate))
			.Returns(Array.Empty<PromotionReliefDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Is(DateTimeOffset.Parse("2023-06-01 00:00:00+08").UtcDateTime),
				Arg.Is(DateTimeOffset.Parse("2023-07-01 00:00:00+08").UtcDateTime));
	}

	[Test]
	public async Task 輸入日期_發放每月救援金_With_Negative_8_Of_Time_Zone_For_First_Day()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2023-07-05 00:00:00-08");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate =
			new DateTimeOffset(DateOnly.Parse("2023-06-01").ToDateTime(TimeOnly.MinValue),
				TimeSpan.FromMinutes(-480));
		var firstDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420,
				"8": -480
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(timeSpanMinutes);

        _ = fakeTgsRepository.CheckHaveReliefDistributeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
            .Returns(false);

        _ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>()).Returns(new[]
		{
			brandId
		}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.GetPromotionUsedAmountAsync(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>())
			.Returns(0m);

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-06-01 00:00:00-08").UtcDateTime,
						DateTimeOffset.Parse("2023-06-30 00:00:00-08").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(firstDate))
			.Returns(Array.Empty<PromotionReliefDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Is(DateTimeOffset.Parse("2023-06-01 00:00:00-08").UtcDateTime),
				Arg.Is(DateTimeOffset.Parse("2023-07-01 00:00:00-08").UtcDateTime));
	}

	[Test]
	public async Task 輸入日期_發放每月救援金_With_Negative_8_Of_Time_Zone_For_Last_Day()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2023-07-02 00:00:00-08");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate =
			new DateTimeOffset(DateOnly.Parse("2023-06-01").ToDateTime(TimeOnly.MinValue),
				TimeSpan.FromMinutes(-480));
		var firstDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420,
				"8": -480
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(
				Arg.Any<string>())
			.Returns(timeSpanMinutes);

        _ = fakeTgsRepository.CheckHaveReliefDistributeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
            .Returns(false);

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

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-06-01 00:00:00-08").UtcDateTime,
						DateTimeOffset.Parse("2023-06-30 00:00:00-08").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(firstDate))
			.Returns(Array.Empty<PromotionReliefDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Is(DateTimeOffset.Parse("2023-06-01 00:00:00-08").UtcDateTime),
				Arg.Is(DateTimeOffset.Parse("2023-07-01 00:00:00-08").UtcDateTime));
	}

	[Test]
	public async Task 輸入日期_發放每月救援金_With_Negative_8_Of_Time_Zone_For_Middle_Day()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new DistributeMonthlyReliefBonusCommandService(fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeMonthlyReliefBonusCommandService>.Instance);

		var scheduleTime = DateTimeOffset.Parse("2023-07-12 00:00:00-08");

		var command = new DistributeMonthlyReliefBonusCommand(scheduleTime.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate =
			new DateTimeOffset(DateOnly.Parse("2023-06-01").ToDateTime(TimeOnly.MinValue),
				TimeSpan.FromMinutes(-480));
		var firstDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420,
				"8": -480
			}
			""";

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(
				Arg.Any<string>())
			.Returns(timeSpanMinutes);

        _ = fakeTgsRepository.CheckHaveReliefDistributeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
            .Returns(false);

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
				Arg.Is("ReliefMoneyMonthly"))
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new(DateTimeOffset.Parse("2023-06-01 00:00:00-08").UtcDateTime,
						DateTimeOffset.Parse("2023-06-30 00:00:00-08").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetReliefUnDistributedAsync(Arg.Is(promotionId), Arg.Is(firstDate))
			.Returns(Array.Empty<PromotionReliefDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionReliefSettlementsAsync(
				Arg.Any<Guid>(),
				Arg.Is(DateTimeOffset.Parse("2023-06-01 00:00:00-08").UtcDateTime),
				Arg.Is(DateTimeOffset.Parse("2023-07-01 00:00:00-08").UtcDateTime));
	}
}
