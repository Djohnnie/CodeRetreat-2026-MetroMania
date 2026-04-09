using MetroMania.Application.Interfaces;

namespace MetroMania.Orleans.Client;

public class OrleansScriptValidationService(IClusterClient clusterClient) : IScriptValidationService
{
    public async Task<ScriptValidationResult> ValidateAsync(string base64Code)
    {
        // Use a new grain key each time for isolation — if the script causes a crash,
        // it only takes down that specific grain activation, not the whole silo.
        var grain = clusterClient.GetGrain<IScriptValidationGrain>(Guid.NewGuid());
        var result = await grain.ValidateScriptAsync(base64Code);
        return new ScriptValidationResult(result.Success, result.Errors);
    }
}
