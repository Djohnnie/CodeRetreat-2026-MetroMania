namespace MetroMania.Application.Interfaces;

public interface IRenderBlobStorage
{
    Task UploadAsync(string blobName, string content, CancellationToken ct = default);
    Task<string> DownloadAsync(string blobName, CancellationToken ct = default);
}
