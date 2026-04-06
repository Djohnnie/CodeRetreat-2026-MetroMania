using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MetroMania.Infrastructure.BlobStorage;

public class BlobRepository
{
    private readonly BlobContainerClient _container;

    public BlobRepository(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task UploadAsync(string blobName, string content, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blobClient = _container.GetBlobClient(blobName);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true, ct);
    }

    public async Task UploadBytesAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blobClient = _container.GetBlobClient(blobName);
        using var stream = new MemoryStream(content);
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
    }

    public async Task<string> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }

    public async Task<byte[]> DownloadBytesAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync(ct);
        return response.Value.Content.ToArray();
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
