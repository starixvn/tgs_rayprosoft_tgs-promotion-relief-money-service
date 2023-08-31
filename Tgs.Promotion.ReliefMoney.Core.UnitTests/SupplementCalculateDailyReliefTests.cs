using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.Promotion.ReliefMoney.Core.ApplicationServices;

namespace Tgs.Promotion.ReliefMoney.Core.UnitTests;

public class SupplementCalculateDailyReliefTests
{
    [Test]
    public async Task 輸入指定日期_補統計每日救援金()
    {
        // Arrange
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        var sut = new SupplementCalculateDailyReliefCommandService(
            fakeTgsRepository,
            fakePromotionServiceClient,
            fakeBrandServiceClient,
            NullLogger<SupplementCalculateDailyReliefCommandService>.Instance);

        var brandId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var date = DateTimeOffset.Parse("2023-11-29 16:00:00+00");
        var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420
			}
			""";

        var command = new SupplementCalculateDailyReliefCommand(date.UtcDateTime, "ReliefMoneyWeekly");

        _ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
            .Returns(timeSpanMinutes);

        _ = fakeTgsRepository.QueryGameRoundStatisticsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<Guid[]>()).Returns(
            new GameRoundStatistics[]
            {
                new() { BrandId = brandId }
            }.ToAsyncEnumerable());

        _ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Any<string>()).Returns(
            new PromotionMainInfo[]
            {
                new()
                {
                    Id = promotionId,
                    BrandId = brandId,
                    EventDuring = new()
                    {
                        From = DateTime.Parse("2023-10-29 16:00:00+00"),
                        To = DateTime.Parse("2023-12-29 16:00:00+00")
                    }
                }
            }.ToAsyncEnumerable());

        _ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>())
            .Returns(new[]
            {
                brandId
            }.ToAsyncEnumerable());

        // Act
        await sut.ExecuteAsync(command);

        var reportDateTime = new DateTimeOffset(command.SettleDateTime).ToOffset(TimeSpan.FromMinutes(480)).AddDays(-1);

        var reportDate = new DateTimeOffset(reportDateTime.Year, reportDateTime.Month, reportDateTime.Day, 0, 0,
            0, reportDateTime.Offset);

        // Assert
        _ = fakeTgsRepository.Received(1)
            .QueryGameRoundStatisticsAsync(Arg.Is(reportDate), Arg.Is<Guid[]>(x => new[]
            {
                brandId
            }.SequenceEqual(x)));

        _ = fakeTgsRepository.Received(1)
            .GetPlayerCodesByDailyStatisticsReportDateAsync(Arg.Is(reportDate), Arg.Is<IEnumerable<Guid>>(x => new[]
            {
                promotionId
            }.SequenceEqual(x)));

        _ = fakePromotionServiceClient.Received(1)
            .ListPromotionsByPromotionTypeAsync(Arg.Is("ReliefMoneyWeekly"));

        _ = fakeTgsRepository.Received(1)
            .SavePromotionReliefSettlementsAsync(reportDate, Arg.Any<IEnumerable<PromotionReliefSettlement>>());
    }
}