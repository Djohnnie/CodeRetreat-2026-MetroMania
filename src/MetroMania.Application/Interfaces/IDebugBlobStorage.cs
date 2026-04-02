namespace MetroMania.Application.Interfaces;

public interface IDebugBlobStorage
{
    Task UploadAsync(string blobName, string content, CancellationToken ct = default);
}
