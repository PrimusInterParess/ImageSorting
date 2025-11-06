using ImageSorting.API.Models;
using ImageSorting.Core.Interfaces;
using ImageSorting.Core.Models;
using ImageSorting.Data;
using ImageSorting.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using File = ImageSorting.Data.Entities.File;

namespace ImageSorting.API.Services
{
	public class UploadOrchestrationService : IUploadOrchestrationService
	{
		private readonly IBlobUploadService _uploadService;
		private readonly IFileMetadataService _metadataService;
		private readonly ImageSortingContext _db;

		public UploadOrchestrationService(
			IBlobUploadService uploadService,
			IFileMetadataService metadataService,
			ImageSortingContext db)
		{
			_uploadService = uploadService;
			_metadataService = metadataService;
			_db = db;
		}

		public async Task<IReadOnlyList<BlobUploadItemWithMetadata>> UploadAndPersistAsync(string container, string? prefix, IFormFileCollection files, CancellationToken ct)
		{
			if (files == null || files.Count == 0)
			{
				return Array.Empty<BlobUploadItemWithMetadata>();
			}

			IReadOnlyList<FileMetadataDto> metadataList = await _metadataService.ExtractAsync(files, ct);

            if (files.Count != metadataList.Count)
            {
                throw new InvalidOperationException("Uploaded items count does not match extracted metadata count.");
            }

            IReadOnlyList<string> blobNames = await _uploadService.UploadAsync(container, prefix, files, ct);

			var nowUtc = DateTime.UtcNow;
			var responses = new List<BlobUploadItemWithMetadata>(blobNames.Count);

			await using var tx = await _db.Database.BeginTransactionAsync(ct);
			try
			{
				for (int i = 0; i < blobNames.Count; i++)
				{
					ct.ThrowIfCancellationRequested();
					var blobName = blobNames[i];
					var meta = metadataList[i];
					var formFile = files[i];

					var fileEntity = new File
					{
						Name = blobName,
						Size = formFile.Length,
						BlobContainerName = container,
						Prefix = prefix ?? string.Empty,
						Exctention = meta.Extension ?? System.IO.Path.GetExtension(blobName),
						DateTaken = meta.BestDateTakenUtc ?? nowUtc,
						DateUploaded = nowUtc,
						DateModified = nowUtc,
						Metadata = new FileMetadata
						{
							BestDateTakenUtc = meta.BestDateTakenUtc,
							BestDateSource = meta.BestDateSource,
							Width = meta.Width,
							Height = meta.Height,
							CameraMake = meta.CameraMake,
							CameraModel = meta.CameraModel,
							ContentType = meta.ContentType,
							Extension = meta.Extension
						}
					};

					_db.Files.Add(fileEntity);
				}

				await _db.SaveChangesAsync(ct);
				await tx.CommitAsync(ct);
			}
			catch
			{
				await tx.RollbackAsync(ct);
				throw;
			}

			for (int i = 0; i < blobNames.Count; i++)
			{
				responses.Add(new BlobUploadItemWithMetadata(blobNames[i], metadataList[i]));
			}

			return responses;
		}
	}
}


