using MetroMania.Domain.Entities;

namespace MetroMania.Engine.Scripting;

/// <summary>
/// The global variables and methods available to player scripts.
/// This is the API surface that bot code can access when executed.
/// </summary>
public class ScriptGlobals
{
    public Level Level { get; }

    public ScriptGlobals(Level level)
    {
        Level = level;
    }
}