namespace Tgs.Promotion.ReliefMoney.Core.ApplicationServices;

public record PayOutCommand(Guid PlanId, Guid DistributeId);