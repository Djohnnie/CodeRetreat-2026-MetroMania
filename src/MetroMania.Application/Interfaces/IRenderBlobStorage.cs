namespace MetroMania.Application.Interfaces;

public interface IRenderBlobStorage
{
    Task UploadAsync(string blobName, string content, CancellationToken ct = default);
    Task UploadBytesAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default);
    Task<string> DownloadAsync(string blobName, CancellationToken ct = default);
    Task<byte[]> DownloadBytesAsync(string blobName, CancellationToken ct = default);
    Task DeleteAsync(string blobName, CancellationToken ct = default);
}
