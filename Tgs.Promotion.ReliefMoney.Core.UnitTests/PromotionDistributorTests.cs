using ExpectedObjects;
using NSubstitute;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.ReliefMoney.Core.UnitTests;

public class PromotionDistributorTests
{
	[Test]
	public async Task 活動救援金發放如果Bonus超過最大發放值則以最大值為主()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			PromotionLimitAmount = null
		};

		var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var firstDate = DateOnly.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
            new[] { "test1" },
            fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),123));

		_ = fakeTgsRepository.QueryPromotionReliefSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate.UtcDateTime),
			Arg.Is(scheduleEndDate.AddDays(1).UtcDateTime))
			.Returns(new[] {
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new ReliefSettings(
						null,
						null,
						100M,
						500M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		var expectedDistributes = new[]
		{
			new
			{
				DistributeDate = firstDate,
				PlayerCode = "test1",
				Bonus = 500M,
				BucketAmount = 5000M
			}
		}.ToExpectedObject();

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SavePromotionReliefDistributesAsync(
				Arg.Is<IEnumerable<PromotionReliefDistribute>>(
					dists => expectedDistributes.Matches(dists)));
	}

	[Test]
	public async Task 活動救援金發放會依照每日統計執行發放()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			PromotionLimitAmount = null
		};
		var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var firstDate = DateOnly.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
            new[] { "test1" },
            fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

		_ = fakeTgsRepository.QueryPromotionReliefSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate.UtcDateTime),
			Arg.Is(scheduleEndDate.UtcDateTime.AddDays(1)))
			.Returns(new[] {
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new ReliefSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		var expectedDistributes = new[]
		{
			new
			{
				DistributeDate = firstDate,
				PlayerCode = "test1",
				Bonus = 1000M,
				BucketAmount = 10000M
			}
		}.ToExpectedObject();

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SavePromotionReliefDistributesAsync(
				Arg.Is<IEnumerable<PromotionReliefDistribute>>(
					dists => expectedDistributes.Matches(dists)));
	}

    [Test]
    public async Task 活動救援金發放會依照每日統計執行發放_When_No_Auto_Join_Promotion()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            PromotionLimitAmount = null
        };
        var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
        var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
        var firstDate = DateOnly.Parse("2022-12-7");

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();
        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionDistributor(
            promotionMainInfo,
            scheduleStartDate,
            scheduleEndDate,
            0M,
            new[] { "test1" },
            fakeSerialNumberGenerator,
            fakeMessageSender,
            fakePromotionServiceClient);

        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        _ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

        _ = fakeTgsRepository.QueryPromotionReliefSettlementsAsync(
            Arg.Is(promotionMainInfo.Id),
            Arg.Is(scheduleStartDate.UtcDateTime),
            Arg.Is(scheduleEndDate.UtcDateTime.AddDays(1)))
            .Returns(new[] {
                new PromotionReliefSettlement(
                    "test1",
                    Guid.NewGuid(),
                    promotionMainInfo.Id,
                    10000M,
                    1000M,
                    10000M,
                    new ReliefSettings(
                        null,
                        null,
                        100M,
                        50000M,
                        10M,
                        10M,
                        false,
                        Array.Empty<int>()))
            }.ToAsyncEnumerable());

        var expectedDistributes = new[]
        {
            new
            {
                DistributeDate = firstDate,
                PlayerCode = "test1",
                Bonus = 1000M,
                BucketAmount = 10000M
            }
        }.ToExpectedObject();

        // Act
        await sut.ProcessDistributesAsync(fakeTgsRepository);

        // Assert
        _ = fakeTgsRepository.Received(1)
            .SavePromotionReliefDistributesAsync(
                Arg.Is<IEnumerable<PromotionReliefDistribute>>(
                    dists => expectedDistributes.Matches(dists)));
    }

    [Test]
    public async Task No_Send_The_SOS_If_Has_No_Participant_And_Has_No_Auto_Join_Promotion()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid(),
            PromotionLimitAmount = null
        };
        var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
        var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
        var firstDate = DateOnly.Parse("2022-12-7");

        var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
        var fakeMessageSender = Substitute.For<IMessageSender>();
        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionDistributor(
            promotionMainInfo,
            scheduleStartDate,
            scheduleEndDate,
            0M,
            new[] { "test12" },
            fakeSerialNumberGenerator,
            fakeMessageSender,
            fakePromotionServiceClient);

        var fakeTgsRepository = Substitute.For<ITgsRepository>();

        _ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

        _ = fakeTgsRepository.QueryPromotionReliefSettlementsAsync(
            Arg.Is(promotionMainInfo.Id),
            Arg.Is(scheduleStartDate.UtcDateTime),
            Arg.Is(scheduleEndDate.UtcDateTime.AddDays(1)))
            .Returns(new[] {
                new PromotionReliefSettlement(
                    "test1",
                    Guid.NewGuid(),
                    promotionMainInfo.Id,
                    10000M,
                    1000M,
                    10000M,
                    new ReliefSettings(
                        null,
                        null,
                        100M,
                        50000M,
                        10M,
                        10M,
                        false,
                        Array.Empty<int>()))
            }.ToAsyncEnumerable());

        var expectedDistributes = new[]
        {
            new
            {
                DistributeDate = firstDate,
                PlayerCode = "test1",
                Bonus = 1000M,
                BucketAmount = 10000M
            }
        }.ToExpectedObject();

        // Act
        await sut.ProcessDistributesAsync(fakeTgsRepository);

        // Assert
        _ = fakeTgsRepository.Received(0)
            .SavePromotionReliefDistributesAsync(
                Arg.Is<IEnumerable<PromotionReliefDistribute>>(
                    dists => expectedDistributes.Matches(dists)));
    }

    [Test]
	public async Task 活動救援金發放會依照日期區間統計做發放()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			PromotionLimitAmount = null
		};
		var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var firstDate = DateOnly.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
            new[] { "test1" },
            fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

		_ = fakeTgsRepository.QueryPromotionReliefSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate.UtcDateTime),
			Arg.Is(scheduleEndDate.UtcDateTime.AddDays(1)))
			.Returns(new[] {
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new ReliefSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new ReliefSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new ReliefSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new ReliefSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new ReliefSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new ReliefSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		var expectedDistributes = new[]
		{
			new
			{
				DistributeDate = firstDate,
				PlayerCode = "test1",
				Bonus = 1500M,
				BucketAmount = 15000M
			}
		}.ToExpectedObject();

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SavePromotionReliefDistributesAsync(
				Arg.Is<IEnumerable<PromotionReliefDistribute>>(
					dists => expectedDistributes.Matches(dists)));
	}

	[Test]
	public async Task 活動救援金發放如果有效投注未達門檻不會計算進去()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			PromotionLimitAmount = null
		};
		var scheduleStartDate = DateTime.Parse("2022-12-7");
		var scheduleEndDate = DateTime.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
            new[] { "test1" },
            fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

		_ = fakeTgsRepository.GetReliefUnDistributedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
			.Returns(Array.Empty<PromotionReliefDistribute>());

		_ = fakeTgsRepository.QueryPromotionReliefSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate),
			Arg.Is(scheduleEndDate.AddDays(1)))
			.Returns(new[] {
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new ReliefSettings(
						null,
						null,
						20000M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(0)
			.SavePromotionReliefDistributesAsync(
				Arg.Any<IEnumerable<PromotionReliefDistribute>>());

		_ = fakeTgsRepository.Received(0)
			.GetReliefUnDistributedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>());
	}

	[Test]
	public async Task 活動救援金發放如果已超過發放上限則不派發彩金()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			PromotionLimitAmount = 1000M
		};
		var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+08");
		var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+08");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			1200M,
            new[] { "test1" },
            fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

		_ = fakeTgsRepository.GetReliefUnDistributedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
			.Returns(Array.Empty<PromotionReliefDistribute>());

		_ = fakeTgsRepository.QueryPromotionReliefSettlementsAsync(
                Arg.Is(promotionMainInfo.Id),
                Arg.Is(scheduleStartDate.UtcDateTime),
                Arg.Is(scheduleEndDate.AddDays(1).UtcDateTime))
			.Returns(new[] {
				new PromotionReliefSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new ReliefSettings(
						null,
						null,
						10000M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SavePromotionReliefDistributesAsync(
				Arg.Is<IEnumerable<PromotionReliefDistribute>>(s => s.First().Bonus == 0));

		_ = fakeTgsRepository.Received(0)
			.GetReliefUnDistributedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>());
	}
}
