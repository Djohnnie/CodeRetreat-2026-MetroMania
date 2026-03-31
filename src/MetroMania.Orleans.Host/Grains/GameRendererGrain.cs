using System.Text.Json;
using Microsoft.CodeAnalysis;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Extensions;
using MetroMania.Engine;
using MetroMania.Engine.Model;
using MetroMania.Engine.Scripting;
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

            // Validate script by compiling for diagnostics using the run wrapper
            var runWrapper = WrapInRunScript(base64Code);
            var runCompiler = new ScriptCompiler<GameResult>();

            var diagnostics = await runCompiler.CompileForDiagnostics(runWrapper);
            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();

            if (errors.Count > 0)
                return new ScriptRenderResult { Success = false, Error = string.Join("; ", errors) };

            // Run full game to determine how many days were survived
            var runScript = await runCompiler.CompileForExecution(runWrapper);
            var fullResult = await runScript.Invoke(new ScriptGlobals(level));

            if (fullResult is null)
                return new ScriptRenderResult { Success = false, Error = "Script returned null." };

            int totalDays = fullResult.DaysSurvived;

            // Compile the snapshot wrapper (returns GameSnapshot instead of GameResult)
            var snapshotWrapper = WrapInSnapshotScript(base64Code);
            var snapshotCompiler = new ScriptCompiler<GameSnapshot>();
            var snapshotScript = await snapshotCompiler.CompileForExecution(snapshotWrapper);

            var engine = new MetroManiaEngine();
            var renderer = new MetroManiaRenderer(engine, svgResourcesPath);
            var renders = new List<FrameRender>();

            // Render one frame per in-game day
            for (int day = 1; day <= totalDays; day++)
            {
                var snapshot = await snapshotScript.Invoke(new ScriptGlobals(level, day * 24));
                if (snapshot is null) continue;

                var svg = renderer.RenderSnapshot(level, snapshot);
                renders.Add(new FrameRender { Day = day, SvgContent = svg });
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

    private static string WrapInRunScript(string base64PlayerCode)
    {
        var playerCode = base64PlayerCode.Base64Decode();

        var outerScript = """
            var engine = new MetroManiaEngine();
            var runner = new MyMetroManiaRunner();
            var result = engine.Run(runner, Level);
            return result;

            <<PLACEHOLDER>>
            """;

        return outerScript.Replace("<<PLACEHOLDER>>", playerCode).Base64Encode();
    }

    private static string WrapInSnapshotScript(string base64PlayerCode)
    {
        var playerCode = base64PlayerCode.Base64Decode();

        var outerScript = """
            var engine = new MetroManiaEngine();
            var runner = new MyMetroManiaRunner();
            var snapshot = engine.RunForHours(runner, Level, TargetHours);
            return snapshot;

            <<PLACEHOLDER>>
            """;

        return outerScript.Replace("<<PLACEHOLDER>>", playerCode).Base64Encode();
    }
}
