namespace ImageSorting.Core.Interfaces
{
    public record SortRequest(string SourcePath, string DestinationPath, bool MoveFiles, string? LogDirectory = null);
    public record SortResult(int Moved, int Skipped, int Errors, string? LogFilePath);

    public interface IImageSortingService
    {
        Task<SortResult> SortAsync(SortRequest request, CancellationToken cancellationToken);
    }
}
