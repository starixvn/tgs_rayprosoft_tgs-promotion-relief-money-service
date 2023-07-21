namespace Tgs.Promotion.ReliefMoney.Core;

public static class DateTimeOffsetExtensions
{
	public static DateTimeOffset GetDayOfWeek(this DateTimeOffset dateTimeOffset, int day) =>
		dateTimeOffset.Subtract(new TimeSpan((int)dateTimeOffset.DayOfWeek - day, 0, 0, 0));

	public static DateTimeOffset GetFirstDayOfMonth(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.AddDays(1 - dateTimeOffset.Day);

	public static DateTimeOffset GetLastDayOfMonth(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.GetFirstDayOfMonth().AddMonths(1).AddDays(-1);

	public static DateTimeOffset GetMonday(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.Subtract(new TimeSpan(((int)dateTimeOffset.DayOfWeek == 0 ? 7 : (int)dateTimeOffset.DayOfWeek) - 1, 0, 0, 0));

	public static DateTimeOffset GetSunday(this DateTimeOffset dateTimeOffset) =>
		dateTimeOffset.Subtract(new TimeSpan(((int)dateTimeOffset.DayOfWeek == 0 ? 7 : (int)dateTimeOffset.DayOfWeek) - 7, 0, 0, 0));
}