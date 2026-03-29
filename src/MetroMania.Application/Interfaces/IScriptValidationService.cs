namespace MetroMania.Application.Interfaces;

public interface IScriptValidationService
{
    Task<ScriptValidationResult> ValidateAsync(string base64Code);
}

public record ScriptValidationResult(bool Success, IReadOnlyList<string> Errors);
