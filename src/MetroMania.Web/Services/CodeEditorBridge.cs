namespace MetroMania.Web.Services;

/// <summary>
/// Scoped (circuit-lifetime) bridge that lets the Conductor panel
/// read from and write to the Monaco code editor on the Play page.
/// The Play page registers its delegates when mounted and clears them on dispose.
/// </summary>
public sealed class CodeEditorBridge
{
    /// <summary>Set by the Play page — returns the current editor content.</summary>
    public Func<Task<string?>>? GetCode { get; set; }

    /// <summary>Set by the Play page — replaces the editor content.</summary>
    public Func<string, Task>? SetCode { get; set; }

    /// <summary>True when the Play page has registered its delegates.</summary>
    public bool IsAvailable => GetCode is not null && SetCode is not null;
}
