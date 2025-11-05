namespace ImageSorting.API.Models
{
    public record BlobSortRequest(
        string SourceContainer,
        string? SourcePrefix,
        string DestinationContainer,
        bool MoveBlobs,
        string? LogContainer,
        string? LogPrefix
    );

    public record BlobSortResult(
        int Moved,
        int Skipped,
        int Errors,
        string? LogBlobPath
    );
}


