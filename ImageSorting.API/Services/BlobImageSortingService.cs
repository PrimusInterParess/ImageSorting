using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageSorting.API.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using System.Globalization;
using System.Text;

namespace ImageSorting.API.Services
{
    public interface IBlobImageSortingService
    {
        Task<BlobSortResult> SortAsync(BlobSortRequest request, CancellationToken cancellationToken);
    }

    public class BlobImageSortingService : IBlobImageSortingService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobImageSortingService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task<BlobSortResult> SortAsync(BlobSortRequest request, CancellationToken cancellationToken)
        {
            var source = _blobServiceClient.GetBlobContainerClient(request.SourceContainer);
            var dest = _blobServiceClient.GetBlobContainerClient(request.DestinationContainer);

            await dest.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var moved = 0;
            var skipped = 0;
            var errors = 0;

            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"Source: {request.SourceContainer}/{request.SourcePrefix}");
            logBuilder.AppendLine($"Destination: {request.DestinationContainer}");
            logBuilder.AppendLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logBuilder.AppendLine(string.Empty);

            await foreach (var item in source.GetBlobsAsync(prefix: request.SourcePrefix, cancellationToken: cancellationToken))
            {
                // Skip directories (virtual) — blobs only
                if (item.Properties.ContentLength == 0 && item.Name.EndsWith("/", StringComparison.Ordinal))
                {
                    continue;
                }

                var blobClient = source.GetBlobClient(item.Name);
                try
                {
                    var createdDate = await GetBestCreationDateAsync(blobClient, item, cancellationToken);
                    var destPrefix = GetYearMonthPrefix(createdDate);
                    var fileName = Path.GetFileName(item.Name);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        // For blobs without a filename segment, use the last path segment
                        fileName = item.Name.TrimEnd('/').Split('/').LastOrDefault() ?? "file";
                    }

                    var destName = await GetUniqueDestinationBlobNameAsync(dest, destPrefix, fileName, cancellationToken);
                    var destBlob = dest.GetBlobClient(destName);

                    // Stream copy to avoid SAS needs
                    var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
                    await destBlob.UploadAsync(download.Value.Content, new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders { ContentType = item.Properties.ContentType }
                    }, cancellationToken);

                    if (request.MoveBlobs)
                    {
                        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
                    }

                    moved++;
                    logBuilder.AppendLine($"Moved: {item.Name} -> {destName}");
                }
                catch (Exception ex)
                {
                    errors++;
                    logBuilder.AppendLine($"Error: {item.Name} | Reason: {ex.Message}");
                }
            }

            logBuilder.AppendLine(string.Empty);
            logBuilder.AppendLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logBuilder.AppendLine($"Summary -> Moved: {moved}, Skipped: {skipped}, Errors: {errors}");

            string? logBlobPath = null;
            if (!string.IsNullOrWhiteSpace(request.LogContainer))
            {
                var logContainer = _blobServiceClient.GetBlobContainerClient(request.LogContainer);
                await logContainer.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

                var sourceBase = request.SourcePrefix?.TrimEnd('/');
                var lastSeg = string.IsNullOrWhiteSpace(sourceBase) ? "root" : sourceBase!.Split('/').Last();
                var sanitized = SanitizeForFileName(lastSeg);
                var logName = $"{moved} - {sanitized}.txt";
                var prefix = string.IsNullOrWhiteSpace(request.LogPrefix) ? "logs" : request.LogPrefix!.Trim('/');
                var logBlobName = string.IsNullOrWhiteSpace(prefix) ? logName : $"{prefix}/{logName}";

                var logBlob = logContainer.GetBlobClient(logBlobName);
                using var logStream = new MemoryStream(Encoding.UTF8.GetBytes(logBuilder.ToString()));
                await logBlob.UploadAsync(logStream, overwrite: true, cancellationToken);
                logBlobPath = $"{request.LogContainer}/{logBlobName}";
            }

            return new BlobSortResult(moved, skipped, errors, logBlobPath);
        }

        private static async Task<DateTime> GetBestCreationDateAsync(BlobClient blob, BlobItem item, CancellationToken ct)
        {
            // Try metadata extraction from content stream
            try
            {
                var download = await blob.DownloadStreamingAsync(cancellationToken: ct);
                using var content = download.Value.Content;
                var directories = ImageMetadataReader.ReadMetadata(content);

                if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal), out var dt) && dt.HasValue) return dt.Value;
                if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeDigitized), out dt) && dt.HasValue) return dt.Value;
                if (TryGetDate(() => directories.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTime), out dt) && dt.HasValue) return dt.Value;
                if (TryGetDate(() => directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault()?.GetDateTime(QuickTimeMovieHeaderDirectory.TagCreated), out dt) && dt.HasValue) return dt.Value;
                if (TryGetDate(() => directories.OfType<QuickTimeTrackHeaderDirectory>().FirstOrDefault()?.GetDateTime(QuickTimeTrackHeaderDirectory.TagCreated), out dt) && dt.HasValue) return dt.Value;
            }
            catch
            {
                // ignore and fallback
            }

            // Fallback to blob last modified
            return item.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow;
        }

        private static bool TryGetDate(Func<DateTime?> getter, out DateTime? value)
        {
            try
            {
                value = getter();
                return value.HasValue;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        private static string GetYearMonthPrefix(DateTime timestamp)
        {
            var year = timestamp.Year.ToString("D4");
            var monthNumber = timestamp.Month;
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNumber);
            var monthFolder = $"{monthNumber:D2} - {monthName}";
            return $"{year}/{monthFolder}";
        }

        private static async Task<string> GetUniqueDestinationBlobNameAsync(BlobContainerClient container, string destPrefix, string fileName, CancellationToken ct)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var candidate = string.IsNullOrWhiteSpace(destPrefix) ? fileName : $"{destPrefix}/{fileName}";
            var blob = container.GetBlobClient(candidate);
            if (!await blob.ExistsAsync(ct))
            {
                return candidate;
            }
            int counter = 1;
            while (true)
            {
                var newName = $"{baseName} ({counter}){ext}";
                var attempt = string.IsNullOrWhiteSpace(destPrefix) ? newName : $"{destPrefix}/{newName}";
                var attemptBlob = container.GetBlobClient(attempt);
                if (!await attemptBlob.ExistsAsync(ct))
                {
                    return attempt;
                }
                counter++;
            }
        }

        private static string SanitizeForFileName(string input)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(input.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return sanitized;
        }
    }
}


