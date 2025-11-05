using ImageSorting.Core.Interfaces;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using System.Globalization;
using Directory = System.IO.Directory;
using Microsoft.Extensions.Options;
using ImageSorting.Core.Options;

namespace ImageSorting.Core
{
    public class ImageSortingService : IImageSortingService
    {
        private readonly string? _defaultLogDirectory;

        public ImageSortingService(IOptions<SortingOptions> options)
        {
            _defaultLogDirectory = options?.Value?.DefaultLogDirectory;
        }
        public Task<SortResult> SortAsync(SortRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SourcePath) || !System.IO.Directory.Exists(request.SourcePath))
            {
                throw new DirectoryNotFoundException($"Source path does not exist: {request.SourcePath}");
            }

            if (string.IsNullOrWhiteSpace(request.DestinationPath))
            {
                throw new ArgumentException("Destination path is required.");
            }

            Directory.CreateDirectory(request.DestinationPath);

            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".heic", ".heif", ".dng",
                ".mov", ".mp4", ".avi"
            };

            var logDirectory = !string.IsNullOrWhiteSpace(request.LogDirectory)
                ? request.LogDirectory!
                : (!string.IsNullOrWhiteSpace(_defaultLogDirectory) ? _defaultLogDirectory! : @"D:\\Library\\Documentation for sorting");
            try { System.IO.Directory.CreateDirectory(logDirectory); } catch { }

            var sourceBaseName = Path.GetFileName(Path.TrimEndingDirectorySeparator(request.SourcePath));
            if (string.IsNullOrWhiteSpace(sourceBaseName)) { sourceBaseName = request.SourcePath; }
            var sanitizedSourceName = SanitizeForFileName(sourceBaseName);
            var tempLogFilePath = Path.Combine(logDirectory, $"sorting-{DateTime.Now:yyyyMMdd-HHmmss}-{sanitizedSourceName}.txt");

            var movedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            List<string> files;
            try
            {
                files = System.IO.Directory
                    .EnumerateFiles(request.SourcePath, "*.*", SearchOption.AllDirectories)
                    .Where(path => imageExtensions.Contains(Path.GetExtension(path)))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to enumerate files: {ex.Message}", ex);
            }

            if (files.Count == 0)
            {
                using (var emptyWriter = new StreamWriter(tempLogFilePath, append: true))
                {
                    emptyWriter.WriteLine($"Source: {request.SourcePath}");
                    emptyWriter.WriteLine($"Destination: {request.DestinationPath}");
                    emptyWriter.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    emptyWriter.WriteLine(string.Empty);
                    emptyWriter.WriteLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    emptyWriter.WriteLine($"Summary -> Moved: 0, Skipped: 0, Errors: 0");
                }

                return Task.FromResult(new SortResult(0, 0, 0, tempLogFilePath));
            }

            using (var logWriter = new StreamWriter(tempLogFilePath, append: true))
            {
                logWriter.AutoFlush = true;
                logWriter.WriteLine($"Source: {request.SourcePath}");
                logWriter.WriteLine($"Destination: {request.DestinationPath}");
                logWriter.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine(string.Empty);

                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var creationDate = GetBestCreationDate(filePath);
                        var destinationDirectory = GetDestinationDirectory(request.DestinationPath, creationDate);

                        Directory.CreateDirectory(destinationDirectory);

                        var destinationPath = GetUniqueDestinationPath(destinationDirectory, Path.GetFileName(filePath));

                        if (string.Equals(Path.GetFullPath(filePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                        {
                            skippedCount++;
                            logWriter.WriteLine($"Skipped (same destination): {filePath}");
                            continue;
                        }

                        if (request.MoveFiles)
                        {
                            File.Move(filePath, destinationPath);
                        }
                        else
                        {
                            File.Copy(filePath, destinationPath, overwrite: false);
                        }

                        movedCount++;
                        logWriter.WriteLine($"Moved: {filePath} -> {destinationPath}");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        logWriter.WriteLine($"Error: {filePath} | Reason: {ex.Message}");
                    }
                }

                logWriter.WriteLine(string.Empty);
                logWriter.WriteLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine($"Summary -> Moved: {movedCount}, Skipped: {skippedCount}, Errors: {errorCount}");
            }

            string? finalLogPath = null;
            try
            {
                var finalLogName = $"{movedCount} - {sanitizedSourceName}.txt";
                finalLogPath = GetUniqueFilePath(logDirectory, finalLogName);
                File.Move(tempLogFilePath, finalLogPath);
            }
            catch
            {
                finalLogPath = tempLogFilePath;
            }

            return Task.FromResult(new SortResult(movedCount, skippedCount, errorCount, finalLogPath));
        }

        private static DateTime GetBestCreationDate(string filePath)
        {
            var fromMetadata = TryGetDateFromMetadata(filePath);
            if (fromMetadata.HasValue)
            {
                return fromMetadata.Value;
            }

            var creation = File.GetCreationTime(filePath);
            if (creation == DateTime.MinValue || creation == DateTime.MaxValue)
            {
                return File.GetLastWriteTime(filePath);
            }
            return creation;
        }

        private static DateTime? TryGetDateFromMetadata(string filePath)
        {
            IReadOnlyList<MetadataExtractor.Directory> directories;
            try
            {
                directories = ImageMetadataReader.ReadMetadata(filePath);
            }
            catch
            {
                return null;
            }

            if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal), out var dt) && dt.HasValue)
            {
                return dt.Value;
            }

            if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeDigitized), out dt) && dt.HasValue)
            {
                return dt.Value;
            }

            if (TryGetDate(() => directories.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTime), out dt) && dt.HasValue)
            {
                return dt.Value;
            }

            if (TryGetDate(() => directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault()?.GetDateTime(QuickTimeMovieHeaderDirectory.TagCreated), out dt) && dt.HasValue)
            {
                return dt.Value;
            }

            if (TryGetDate(() => directories.OfType<QuickTimeTrackHeaderDirectory>().FirstOrDefault()?.GetDateTime(QuickTimeTrackHeaderDirectory.TagCreated), out dt) && dt.HasValue)
            {
                return dt.Value;
            }

            return null;
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

        private static string GetDestinationDirectory(string root, DateTime timestamp)
        {
            var year = timestamp.Year.ToString("D4");
            var monthNumber = timestamp.Month;
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNumber);
            var monthFolder = $"{monthNumber:D2} - {monthName}";
            return Path.Combine(root, year, monthFolder);
        }

        private static string GetUniqueDestinationPath(string destinationDirectory, string fileName)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var candidate = Path.Combine(destinationDirectory, fileName);

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            int counter = 1;
            while (true)
            {
                var newName = $"{baseName} ({counter}){extension}";
                var attempt = Path.Combine(destinationDirectory, newName);
                if (!File.Exists(attempt))
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

        private static string GetUniqueFilePath(string directory, string fileName)
        {
            var candidate = Path.Combine(directory, fileName);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            int counter = 1;
            while (true)
            {
                var attempt = Path.Combine(directory, $"{name} ({counter}){ext}");
                if (!File.Exists(attempt))
                {
                    return attempt;
                }
                counter++;
            }
        }
    }
}

