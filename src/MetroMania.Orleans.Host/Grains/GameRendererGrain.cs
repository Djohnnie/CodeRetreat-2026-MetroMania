using System.Text.Json;
using Microsoft.CodeAnalysis;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Extensions;
using MetroMania.Engine;
using MetroMania.Engine.Model;
using MetroMania.Scripting;
using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.Host.Grains;

public class GameRendererGrain(IConfiguration configuration) : Grain, IGameRendererGrain
{
    public async Task<ScriptRenderResult> RenderScriptAsync(string base64Code, string levelDataJson)
    {
        try
        {
            var level = JsonSerializer.Deserialize<Level>(levelDataJson)
                ?? throw new InvalidOperationException("Failed to deserialize level data.");

            var svgResourcesPath = ResolveSvgResourcesPath();

            // Validate script by compiling for diagnostics
            var snapshotWrapper = WrapInSnapshotScript(base64Code);
            var snapshotCompiler = new ScriptCompiler<IReadOnlyList<GameSnapshot>>();

            var diagnostics = await snapshotCompiler.CompileForDiagnostics(snapshotWrapper);
            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();

            if (errors.Count > 0)
                return new ScriptRenderResult { Success = false, Error = string.Join("; ", errors) };

            // Run the simulation once to get all hourly snapshots
            var snapshotScript = await snapshotCompiler.CompileForExecution(snapshotWrapper);
            var hourlySnapshots = await snapshotScript.Invoke(new ScriptGlobals(level));
            if (hourlySnapshots is null)
                return new ScriptRenderResult { Success = false, Error = "Snapshot script returned null." };

            using var renderer = new MetroManiaRenderer(svgResourcesPath);
            var renders = new List<FrameRender>(hourlySnapshots.Count);

            // Render one frame per in-game hour (1-indexed)
            for (int i = 0; i < hourlySnapshots.Count; i++)
            {
                var svg = renderer.RenderSnapshot(level, hourlySnapshots[i]);
                renders.Add(new FrameRender { Hour = i + 1, SvgContent = svg });
            }

            DeactivateOnIdle();

            return new ScriptRenderResult { Success = true, Renders = renders };
        }
        catch (Exception ex)
        {
            return new ScriptRenderResult
            {
                Success = false,
                Error = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    private string ResolveSvgResourcesPath()
    {
        var configured = configuration.GetValue<string>("SVG_RESOURCES_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        // In Docker the resources are copied alongside the app binary at /app/resources
        var dockerPath = Path.Combine(AppContext.BaseDirectory, "resources");
        if (Directory.Exists(dockerPath))
            return dockerPath;

        // Local development fallback: resources/ at repo root, 5 levels above the binary output directory
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "resources"));
    }

    private static string WrapInSnapshotScript(string base64PlayerCode)
    {
        var playerCode = base64PlayerCode.Base64Decode();

        var outerScript = """
            var engine = new MetroManiaEngine();
            var runner = new MyMetroManiaRunner();
            var result = engine.Run(runner, Level, maxHours: Level.LevelData.MaxDays * 24);
            return result;
            
            <<PLACEHOLDER>>
            """;

        return outerScript.Replace("<<PLACEHOLDER>>", playerCode).Base64Encode();
    }
}
