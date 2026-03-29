using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using MetroMania.Domain.Enums;
using MetroMania.Domain.Extensions;
using MetroMania.Engine.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace MetroMania.Engine.Scripting;

public interface IScriptCompiler<TResult>
{
    Task<ScriptRunner<TResult?>> CompileForExecution(string script);
    Task<ImmutableArray<Diagnostic>> CompileForDiagnostics(string script);
}

public class ScriptCompiler<TResult> : IScriptCompiler<TResult>
{
    private static readonly Assembly MSCoreLib = typeof(object).Assembly;
    private static readonly Assembly SystemCore = typeof(Enumerable).Assembly;
    private static readonly Assembly Dynamic = typeof(DynamicAttribute).Assembly;
    private static readonly Assembly EngineModel = typeof(PlayerAction).Assembly;
    private static readonly Assembly DomainEnums = typeof(StationType).Assembly;

    public Task<ScriptRunner<TResult?>> CompileForExecution(string script)
    {
        return Task.Run(() =>
        {
            var botScript = PrepareScript(script);
            return botScript.CreateDelegate();
        });
    }

    public Task<ImmutableArray<Diagnostic>> CompileForDiagnostics(string script)
    {
        return Task.Run(() =>
        {
            var botScript = PrepareScript(script);
            return botScript.Compile();
        });
    }

    private Script<TResult?> PrepareScript(string script)
    {
        var decodedScript = script.Base64Decode();

        var scriptOptions = ScriptOptions.Default
            .AddReferences(MSCoreLib, SystemCore, Dynamic, EngineModel, DomainEnums)
            .WithImports(
                "System", "System.Linq", "System.Collections",
                "System.Collections.Generic", "MetroMania.Domain.Enums",
                "MetroMania.Engine", "MetroMania.Engine.Contracts", "MetroMania.Engine.Model",
                "System.Runtime.CompilerServices")
            .WithOptimizationLevel(OptimizationLevel.Release);

        var botScript = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<TResult?>(
            decodedScript, scriptOptions, typeof(ScriptGlobals));
        botScript.WithOptions(botScript.Options.AddReferences(MSCoreLib, SystemCore));

        return botScript;
    }
}