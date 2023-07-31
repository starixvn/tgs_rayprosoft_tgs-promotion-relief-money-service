using Tgs.Promotion.ReliefMoney.Client;

namespace Tgs.Promotion.ReliefMoney.IntegrationTests;

public class ManualTriggerTest
{
	[Test]
	public async Task Settle()
	{
		await using var web = new TestPromotionApplication();
		var httpClient = web.CreateClient();

		var sut = new PromotionReliefClient(httpClient);

		await sut.SettleAsync(DateOnly.Parse("2022-01-01"), 16);
	}

	[Test]
	public async Task Bonus()
	{
		await using var web = new TestPromotionApplication();
		var httpClient = web.CreateClient();

		var sut = new PromotionReliefClient(httpClient);

		await sut.SendBonusAsync(DateOnly.Parse("2022-01-01"), 16);
	}

}