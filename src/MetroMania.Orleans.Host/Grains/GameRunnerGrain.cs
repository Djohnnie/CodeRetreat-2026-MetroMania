using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Extensions;
using MetroMania.Engine.Model;
using MetroMania.Engine.Scripting;
using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.Host.Grains;

public class GameRunnerGrain : Grain, IGameRunnerGrain
{
    private static readonly JsonSerializerOptions DebugJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false,
        Converters = { new LocationJsonConverter() }
    };

    public Task<string> PingAsync() => Task.FromResult("pong");

    public async Task<ScriptRunResult> RunScriptAsync(string base64Code, string levelDataJson)
    {
        try
        {
            var level = JsonSerializer.Deserialize<Level>(levelDataJson)
                ?? throw new InvalidOperationException("Failed to deserialize level data.");

            var wrappedScript = WrapInOuterScript(base64Code);
            var scriptCompiler = new ScriptCompiler<GameResult>();

            // Check for compilation errors
            var diagnostics = await scriptCompiler.CompileForDiagnostics(wrappedScript);
            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();

            if (errors.Count > 0)
                return new ScriptRunResult
                {
                    Success = false,
                    Error = string.Join("; ", errors)
                };

            // Execute the script with the provided level
            var globals = new ScriptGlobals(level);
            var script = await scriptCompiler.CompileForExecution(wrappedScript);
            var result = await script.Invoke(globals);

            if (result is null)
                return new ScriptRunResult { Success = false, Error = "Script returned null." };

            DeactivateOnIdle(); // Deactivate grain after processing

            return new ScriptRunResult
            {
                Success = true,
                Score = result.TotalScore,
                TimeTakenMs = result.ProcessingTime.TotalMilliseconds,
                DaysSurvived = result.DaysSurvived,
                TotalPassengersSpawned = result.TotalPassengersSpawned,
                DebugJson = ""
            };
        }
        catch (Exception ex)
        {
            return new ScriptRunResult
            {
                Success = false,
                Error = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    private static string WrapInOuterScript(string base64PlayerCode)
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
}