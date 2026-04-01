using MetroMania.Application.Interfaces;

namespace MetroMania.Infrastructure.BlobStorage;

public class RenderBlobStorage(string connectionString)
    : BlobRepository(connectionString, "submission-renders"), IRenderBlobStorage;
