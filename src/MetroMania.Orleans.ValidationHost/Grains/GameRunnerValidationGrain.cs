using Microsoft.CodeAnalysis;
using MetroMania.Domain.Entities;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Extensions;
using MetroMania.Engine.Model;
using MetroMania.Engine.Scripting;
using MetroMania.Orleans.Contracts.Grains;
using MetroMania.Orleans.Contracts.Models;

namespace MetroMania.Orleans.ValidationHost.Grains;

public class GameRunnerValidationGrain : Grain, IGameRunnerValidationGrain
{
    public Task<string> PingAsync() => Task.FromResult("pong");

    public async Task<ScriptValidationResult> ValidateScriptAsync(string base64Code)
    {
        try
        {
            // Wrap the player code in the outer script, just like MetroMania.Demo
            var wrappedScript = WrapInOuterScript(base64Code);

            var scriptCompiler = new ScriptCompiler<GameResult>();

            // Step 1: Check for compilation errors
            var diagnostics = await scriptCompiler.CompileForDiagnostics(wrappedScript);
            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();

            if (errors.Count > 0)
                return new ScriptValidationResult { Success = false, Errors = errors };

            // Step 2: Execute the script with a test level to catch runtime exceptions
            var testLevel = CreateTestLevel();
            var globals = new ScriptGlobals(testLevel);
            var script = await scriptCompiler.CompileForExecution(wrappedScript);
            await script.Invoke(globals);

            DeactivateOnIdle(); // Deactivate grain after processing

            return new ScriptValidationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new ScriptValidationResult
            {
                Success = false,
                Errors = [ex.InnerException?.Message ?? ex.Message]
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

    private static Level CreateTestLevel() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Validation Level",
        Description = "Minimal level used for script validation",
        GridWidth = 4,
        GridHeight = 1,
        LevelData = new LevelData
        {
            Seed = 42,
            Stations =
            [
                new MetroStation
                {
                    GridX = 0, GridY = 0,
                    StationType = StationType.Circle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 1 }]
                },
                new MetroStation
                {
                    GridX = 3, GridY = 0,
                    StationType = StationType.Rectangle,
                    SpawnDelayDays = 0,
                    PassengerSpawnPhases = [new() { AfterDays = 0, FrequencyInHours = 1 }]
                }
            ]
        }
    };
}
