using ImageSorting.Core.Interfaces;
using ImageSorting.Core.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Microsoft.AspNetCore.Http;

namespace ImageSorting.Core.Services
{
	public class FileMetadataService : IFileMetadataService
	{
		public async Task<FileMetadataDto> ExtractAsync(IFormFile file, CancellationToken ct)
		{
			await using var stream = file.OpenReadStream();
			var directories = ImageMetadataReader.ReadMetadata(stream, file.FileName);

			// Determine best date and source
			(string? source, DateTime? date) = GetBestDate(directories);

			// Try to get dimensions (best-effort)
			int? width = null;
			int? height = null;
			try
			{
				var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
				if (subIfd != null)
				{
					if (subIfd.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out var w))
					{
						width = w;
					}
					if (subIfd.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out var h))
					{
						height = h;
					}
				}
			}
			catch { /* ignore */ }

			string? make = null;
			string? model = null;
			try
			{
				var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
				if (ifd0 != null)
				{
					make = ifd0.GetDescription(ExifDirectoryBase.TagMake);
					model = ifd0.GetDescription(ExifDirectoryBase.TagModel);
				}
			}
			catch { /* ignore */ }

			var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType;
			var ext = Path.GetExtension(file.FileName);
			var extension = string.IsNullOrWhiteSpace(ext) ? null : ext.ToLowerInvariant();
			return new FileMetadataDto(
				BestDateTakenUtc: date?.ToUniversalTime(),
				BestDateSource: source,
				Width: width,
				Height: height,
				CameraMake: make,
				CameraModel: model,
				ContentType: contentType,
				Extension: extension
			);
		}

		public async Task<IReadOnlyList<FileMetadataDto>> ExtractAsync(IFormFileCollection files, CancellationToken ct)
		{
			var results = new List<FileMetadataDto>(files.Count);
			foreach (var file in files)
			{
				ct.ThrowIfCancellationRequested();
				results.Add(await ExtractAsync(file, ct));
			}
			return results;
		}

		private static (string? Source, DateTime? Date) GetBestDate(IReadOnlyList<MetadataExtractor.Directory> directories)
		{
			if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal), out var dt) && dt.HasValue)
				return ("EXIF:DateTimeOriginal", dt);
			if (TryGetDate(() => directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTimeDigitized), out dt) && dt.HasValue)
				return ("EXIF:DateTimeDigitized", dt);
			if (TryGetDate(() => directories.OfType<ExifIfd0Directory>().FirstOrDefault()?.GetDateTime(ExifDirectoryBase.TagDateTime), out dt) && dt.HasValue)
				return ("EXIF:DateTime", dt);
			if (TryGetDate(() => directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault()?.GetDateTime(QuickTimeMovieHeaderDirectory.TagCreated), out dt) && dt.HasValue)
				return ("QuickTime:MovieHeaderCreated", dt);
			if (TryGetDate(() => directories.OfType<QuickTimeTrackHeaderDirectory>().FirstOrDefault()?.GetDateTime(QuickTimeTrackHeaderDirectory.TagCreated), out dt) && dt.HasValue)
				return ("QuickTime:TrackHeaderCreated", dt);
			return (null, null);
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
	}
}


