namespace Tgs.Promotion.ReliefMoney.Core;

public record GameRoundStatistics
{
    public string PlayerCode { get; set; }
    public int VipLevel { get; set; }
    public Guid BrandId { get; set; }
    public Guid GameProviderId { get; set; }
    public GameType GameType { get; set; }
    public decimal TotalWinLoss { get; set; }
};