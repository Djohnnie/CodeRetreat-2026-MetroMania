using MetroMania.Application.Interfaces;

namespace MetroMania.Infrastructure.BlobStorage;

public class DebugBlobStorage(string connectionString)
    : BlobRepository(connectionString, "submission-debug"), IDebugBlobStorage;
