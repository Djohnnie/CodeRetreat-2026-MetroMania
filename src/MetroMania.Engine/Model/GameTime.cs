namespace MetroMania.Engine.Model;

/// <summary>
/// Represents the current point in game time: the day number, hour within that day, and the day of the week.
/// </summary>
/// <param name="Day">The 1-indexed day number (day 1 is the first day).</param>
/// <param name="Hour">The hour within the day (0–23).</param>
/// <param name="DayOfWeek">The day of the week.</param>
public readonly record struct GameTime(int Day, int Hour, DayOfWeek DayOfWeek);