using System.Reflection;

namespace MetroMania.Engine;

/// <summary>
/// Provides the embedded viewer HTML template with placeholder replacement.
/// Placeholders: %%LEVEL_TITLE%%, %%TOTAL%%, %%PAD_WIDTH%%
/// </summary>
public static class ViewerTemplate
{
    private static readonly Lazy<string> Template = new(() =>
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MetroMania.Engine.viewer-template.html")
            ?? throw new InvalidOperationException("Embedded viewer-template.html not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    /// <summary>
    /// Returns the viewer HTML with placeholders replaced.
    /// </summary>
    /// <param name="levelTitle">Title of the level.</param>
    /// <param name="totalFrames">Total number of frames/hours.</param>
    /// <param name="padWidth">Zero-padding width for file names (e.g. 4 → 0001.svg, 5 → 00001.svg).</param>
    public static string Generate(string levelTitle, int totalFrames, int padWidth = 4) =>
        Template.Value
            .Replace("%%LEVEL_TITLE%%", levelTitle)
            .Replace("%%TOTAL%%", totalFrames.ToString())
            .Replace("%%PAD_WIDTH%%", padWidth.ToString());
}
