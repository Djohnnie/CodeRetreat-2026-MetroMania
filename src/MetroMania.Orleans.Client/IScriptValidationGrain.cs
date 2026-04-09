using Orleans;

namespace MetroMania.Orleans.Client;

public interface IScriptValidationGrain : IGrainWithGuidKey
{
    Task<OrleansValidationResult> ValidateScriptAsync(string base64Code);
}

[GenerateSerializer]
[Alias("MetroMania.Orleans.Client.OrleansValidationResult")]
public record OrleansValidationResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] IReadOnlyList<string> Errors);
