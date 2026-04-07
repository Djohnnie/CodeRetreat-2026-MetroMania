using System.Text.Json;
using System.Text.Json.Serialization;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Extensions;
using MetroMania.Engine;
using MetroMania.Engine.Model;
using MetroMania.Scripting;
using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.RendererHost.Grains;

public class GameRendererGrain(IConfiguration configuration) : Grain, IGameRendererGrain
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new LocationJsonConverter() }
    };

    public async Task<ScriptRenderResult> RenderBatchAsync(string base64Code, string levelDataJson, int startHour, int count)
    {
        try
        {
            var level = JsonSerializer.Deserialize<Level>(levelDataJson)
                ?? throw new InvalidOperationException("Failed to deserialize level data.");

            var wrappedScript = WrapInOuterScript(base64Code);
            var scriptCompiler = new ScriptCompiler<GameResult>();

            var script = await scriptCompiler.CompileForExecution(wrappedScript);
            var gameResult = await script.Invoke(new ScriptGlobals(level));
            if (gameResult is null)
                return new ScriptRenderResult { Success = false, Error = "Script returned null." };

            var snapshots = gameResult.GameSnapshots;
            using var renderer = new MetroManiaRenderer(ResolveSvgResourcesPath());

            var end = Math.Min(startHour + count, snapshots.Count);
            var renders = new List<FrameRender>(end - startHour);

            for (int i = startHour; i < end; i++)
            {
                var svg = renderer.RenderSnapshot(level, snapshots[i]);
                var json = JsonSerializer.Serialize(snapshots[i], SnapshotJsonOptions);
                renders.Add(new FrameRender { Hour = i + 1, SvgContent = svg, JsonContent = json });
            }

            return new ScriptRenderResult { Success = true, Renders = renders, TotalFrames = snapshots.Count };
        }
        catch (Exception ex)
        {
            return new ScriptRenderResult
            {
                Success = false,
                Error = ex.InnerException?.Message ?? ex.Message
            };
        }
        finally
        {
            DeactivateOnIdle(); // Deactivate grain after processing
        }
    }

    private string ResolveSvgResourcesPath()
    {
        var configured = configuration.GetValue<string>("SVG_RESOURCES_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var dockerPath = Path.Combine(AppContext.BaseDirectory, "resources");
        if (Directory.Exists(dockerPath))
            return dockerPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "resources"));
    }

    private static string WrapInOuterScript(string base64PlayerCode)
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
