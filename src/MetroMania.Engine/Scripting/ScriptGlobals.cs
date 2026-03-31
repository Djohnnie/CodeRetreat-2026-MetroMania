using MetroMania.Domain.Entities;

namespace MetroMania.Engine.Scripting;

/// <summary>
/// The global variables and methods available to player scripts.
/// This is the API surface that bot code can access when executed.
/// </summary>
public class ScriptGlobals
{
    public Level Level { get; }

    /// <summary>
    /// Target number of hours to simulate. Used internally by the renderer grain
    /// to capture a snapshot at a specific point in the game. Player scripts should
    /// not rely on this value; it is 0 during normal scoring runs.
    /// </summary>
    public int TargetHours { get; }

    public ScriptGlobals(Level level, int targetHours = 0)
    {
        Level = level;
        TargetHours = targetHours;
    }
}