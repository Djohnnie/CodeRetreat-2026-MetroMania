using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new LocationJsonConverter() }
    };

    private Level? _level;
    private IReadOnlyList<GameSnapshot>? _snapshots;
    private MetroManiaRenderer? _renderer;

    public async Task<ScriptPrepareResult> PrepareAsync(string base64Code, string levelDataJson)
    {
        try
        {
            _level = JsonSerializer.Deserialize<Level>(levelDataJson)
                ?? throw new InvalidOperationException("Failed to deserialize level data.");

            var wrappedScript = WrapInOuterScript(base64Code);
            var scriptCompiler = new ScriptCompiler<GameResult>();

            var script = await scriptCompiler.CompileForExecution(wrappedScript);
            var gameResult = await script.Invoke(new ScriptGlobals(_level));
            if (gameResult is null)
            {
                Cleanup();
                return new ScriptPrepareResult { Success = false, Error = "Script returned null." };
            }

            _snapshots = gameResult.GameSnapshots;
            _renderer = new MetroManiaRenderer(ResolveSvgResourcesPath());

            return new ScriptPrepareResult { Success = true, TotalFrames = _snapshots.Count };
        }
        catch (Exception ex)
        {
            Cleanup();
            return new ScriptPrepareResult
            {
                Success = false,
                Error = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public Task<ScriptRenderResult> RenderBatchAsync(int startHour, int count)
    {
        if (_level is null || _snapshots is null || _renderer is null)
            return Task.FromResult(new ScriptRenderResult { Success = false, Error = "PrepareAsync must be called first." });

        try
        {
            var end = Math.Min(startHour + count, _snapshots.Count);
            var renders = new List<FrameRender>(end - startHour);

            for (int i = startHour; i < end; i++)
            {
                var svg = _renderer.RenderSnapshot(_level, _snapshots[i]);
                var json = JsonSerializer.Serialize(_snapshots[i], SnapshotJsonOptions);
                renders.Add(new FrameRender { Hour = i + 1, SvgContent = svg, JsonContent = json });
            }

            // If this is the last batch, clean up and deactivate
            if (end >= _snapshots.Count)
            {
                Cleanup();
                DeactivateOnIdle();
            }

            return Task.FromResult(new ScriptRenderResult { Success = true, Renders = renders });
        }
        catch (Exception ex)
        {
            Cleanup();
            DeactivateOnIdle();
            return Task.FromResult(new ScriptRenderResult
            {
                Success = false,
                Error = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    private void Cleanup()
    {
        _renderer?.Dispose();
        _renderer = null;
        _snapshots = null;
        _level = null;
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
