namespace Tgs.Promotion.ReliefMoney.Core.UnitTests;

internal static class ExtensionMethods
{
	internal static DateOnly ToDateOnly(this DateTime date)
		=> DateOnly.FromDateTime(new DateTimeOffset(date).ToOffset(TimeSpan.FromHours(8D)).DateTime);

	internal static bool Between(this DateOnly date, DateOnly from, DateOnly to)
		=> from <= date && to >= date;
}