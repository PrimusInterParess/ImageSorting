using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.QuickTime;

namespace ImageSorting
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Media Sorting - Sort images/videos into Year/Month folders at a destination\nby metadata date with filesystem fallback\n");

            string? sourceRoot = args != null && args.Length > 0 ? args[0] : null;
            string? destinationRoot = args != null && args.Length > 1 ? args[1] : null;
            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                Console.Write("Enter the full path to the folder containing images: ");
                sourceRoot = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(sourceRoot) || !System.IO.Directory.Exists(sourceRoot))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The provided path does not exist. Exiting.");
                Console.ResetColor();
                return;
            }

            if (string.IsNullOrWhiteSpace(destinationRoot))
            {
                Console.Write("Enter the destination folder path (Year/Month folders will be created here): ");
                destinationRoot = Console.ReadLine();
            }
            if (string.IsNullOrWhiteSpace(destinationRoot))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No destination path provided. Exiting.");
                Console.ResetColor();
                return;
            }
            try
            {
                System.IO.Directory.CreateDirectory(destinationRoot!);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to create or access destination path: {ex.Message}");
                Console.ResetColor();
                return;
            }

			var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
				".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".heic", ".heif", ".dng",
				".mov", ".mp4", ".avi"
            };

            Console.WriteLine($"Scanning for media in: {sourceRoot}");
            Console.WriteLine($"Destination: {destinationRoot}\n");

            // Prepare logging
            var logDirectory = @"D:\Library\Documentation for sorting";
            try
            {
                System.IO.Directory.CreateDirectory(logDirectory);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Could not create log directory '{logDirectory}': {ex.Message}");
                Console.ResetColor();
            }
            var sourceBaseName = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(sourceRoot!));
            if (string.IsNullOrWhiteSpace(sourceBaseName))
            {
                sourceBaseName = sourceRoot!;
            }
            var sanitizedSourceName = SanitizeForFileName(sourceBaseName);
            var tempLogFilePath = System.IO.Path.Combine(logDirectory, $"sorting-{DateTime.Now:yyyyMMdd-HHmmss}-{sanitizedSourceName}.txt");

            List<string> files;
            try
            {
                files = System.IO.Directory
                    .EnumerateFiles(sourceRoot, "*.*", System.IO.SearchOption.AllDirectories)
                    .Where(path => imageExtensions.Contains(System.IO.Path.GetExtension(path)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to enumerate files: {ex.Message}");
                Console.ResetColor();
                return;
            }

            if (files.Count == 0)
            {
                Console.WriteLine("No image files found. Nothing to do.");
                return;
            }

            Console.WriteLine($"Found {files.Count} image file(s). Starting sorting...\n");

            int movedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            using (var logWriter = new System.IO.StreamWriter(tempLogFilePath, append: true))
            {
                logWriter.AutoFlush = true;
                logWriter.WriteLine($"Source: {sourceRoot}");
                logWriter.WriteLine($"Destination: {destinationRoot}");
                logWriter.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine(string.Empty);

                foreach (var filePath in files)
                {
                    try
                    {
                        var creationDate = GetBestCreationDate(filePath);
                        var destinationDirectory = GetDestinationDirectory(destinationRoot!, creationDate);

                        // Ensure destination folder exists
                        System.IO.Directory.CreateDirectory(destinationDirectory);

                        var destinationPath = GetUniqueDestinationPath(destinationDirectory, System.IO.Path.GetFileName(filePath));

                        // If the destination is the same as source, skip
                        if (string.Equals(System.IO.Path.GetFullPath(filePath), System.IO.Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                        {
                            skippedCount++;
                            logWriter.WriteLine($"Skipped (same destination): {filePath}");
                            continue;
                        }

                        System.IO.File.Move(filePath, destinationPath);
                        movedCount++;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Moved: {filePath} -> {destinationPath}");
                        Console.ResetColor();
                        logWriter.WriteLine($"Moved: {filePath} -> {destinationPath}");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Skipped (error): {filePath}\n  Reason: {ex.Message}");
                        Console.ResetColor();
                        logWriter.WriteLine($"Error: {filePath} | Reason: {ex.Message}");
                    }
                }

                logWriter.WriteLine(string.Empty);
                logWriter.WriteLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logWriter.WriteLine($"Summary -> Moved: {movedCount}, Skipped: {skippedCount}, Errors: {errorCount}");
            }

            Console.WriteLine("\nDone.");
            Console.WriteLine($"Moved:   {movedCount}");
            Console.WriteLine($"Skipped: {skippedCount}");
            Console.WriteLine($"Errors:  {errorCount}");

            try
            {
                var finalLogName = $"{movedCount} - {sanitizedSourceName}.txt";
                var finalLogPath = GetUniqueFilePath(logDirectory, finalLogName);
                System.IO.File.Move(tempLogFilePath, finalLogPath);
                Console.WriteLine($"Log saved: {finalLogPath}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Could not finalize log file name: {ex.Message}\nTemp log at: {tempLogFilePath}");
                Console.ResetColor();
            }
        }

        private static DateTime GetBestCreationDate(string filePath)
        {
            var fromMetadata = TryGetDateFromMetadata(filePath);
            if (fromMetadata.HasValue)
            {
                return fromMetadata.Value;
            }

            // Fallback to filesystem times
            var creation = System.IO.File.GetCreationTime(filePath);
            if (creation == DateTime.MinValue || creation == DateTime.MaxValue)
            {
                return System.IO.File.GetLastWriteTime(filePath);
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

            // 1) EXIF SubIFD: DateTimeOriginal (when the photo was taken)
            if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal), out var dt) && dt.HasValue)
            {
                return dt.Value;
            }

            // 2) EXIF SubIFD: DateTimeDigitized
            if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeDigitized), out dt) && dt.HasValue)
            {
                return dt.Value;
            }

            // 3) EXIF IFD0: DateTime (often last modified in camera)
            if (TryGetDate(() => directories.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTime), out dt) && dt.HasValue)
            {
                return dt.Value;
            }

            // 4) QuickTime/HEIC: creation dates (common for HEIC/HEIF and some videos)
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
            var monthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNumber);
            var monthFolder = $"{monthNumber:D2} - {monthName}";
            return System.IO.Path.Combine(root, year, monthFolder);
        }

        private static string GetUniqueDestinationPath(string destinationDirectory, string fileName)
        {
            var baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            var extension = System.IO.Path.GetExtension(fileName);
            var candidate = System.IO.Path.Combine(destinationDirectory, fileName);

            if (!System.IO.File.Exists(candidate))
            {
                return candidate;
            }

            int counter = 1;
            while (true)
            {
                var newName = $"{baseName} ({counter}){extension}";
                var attempt = System.IO.Path.Combine(destinationDirectory, newName);
                if (!System.IO.File.Exists(attempt))
                {
                    return attempt;
                }
                counter++;
            }
        }

        private static string SanitizeForFileName(string input)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sanitized = new string(input.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return sanitized;
        }

        private static string GetUniqueFilePath(string directory, string fileName)
        {
            var candidate = System.IO.Path.Combine(directory, fileName);
            if (!System.IO.File.Exists(candidate))
            {
                return candidate;
            }

            var name = System.IO.Path.GetFileNameWithoutExtension(fileName);
            var ext = System.IO.Path.GetExtension(fileName);
            int counter = 1;
            while (true)
            {
                var attempt = System.IO.Path.Combine(directory, $"{name} ({counter}){ext}");
                if (!System.IO.File.Exists(attempt))
                {
                    return attempt;
                }
                counter++;
            }
        }
    }
}
