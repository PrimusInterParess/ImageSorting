using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageSorting.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ImageSorting.Core.Services
{
	public class BlobUploadService : IBlobUploadService
	{
		private readonly BlobServiceClient _blobServiceClient;

		public BlobUploadService(BlobServiceClient blobServiceClient)
		{
			_blobServiceClient = blobServiceClient;
		}

		public async Task<IReadOnlyList<string>> UploadAsync(string container, string? prefix, IFormFileCollection files, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(container))
			{
				throw new ArgumentException("Container is required");
			}

			var containerClient = _blobServiceClient.GetBlobContainerClient(container);
			await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

			var uploaded = new List<string>(capacity: files.Count);
			foreach (var file in files)
			{
				if (file.Length <= 0)
				{
					continue;
				}

				var safeFileName = Path.GetFileName(file.FileName);
				var blobName = await GetUniqueBlobNameAsync(containerClient, prefix, safeFileName, ct);
				var blobClient = containerClient.GetBlobClient(blobName);

				var headers = new BlobHttpHeaders
				{
					ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
				};

				await using var stream = file.OpenReadStream();
				await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, ct);

				uploaded.Add(blobName);
			}

			return uploaded;
		}

		private static async Task<string> GetUniqueBlobNameAsync(BlobContainerClient container, string? prefix, string fileName, CancellationToken ct)
		{
			var baseName = Path.GetFileNameWithoutExtension(fileName);
			var ext = Path.GetExtension(fileName);
			var candidate = string.IsNullOrWhiteSpace(prefix) ? fileName : $"{prefix.TrimEnd('/')}/{fileName}";
			var blob = container.GetBlobClient(candidate);
			if (!await blob.ExistsAsync(ct))
			{
				return candidate;
			}

			int counter = 1;
			while (true)
			{
				var newName = $"{baseName} ({counter}){ext}";
				var attempt = string.IsNullOrWhiteSpace(prefix) ? newName : $"{prefix.TrimEnd('/')}/{newName}";
				var attemptBlob = container.GetBlobClient(attempt);
				if (!await attemptBlob.ExistsAsync(ct))
				{
					return attempt;
				}
				counter++;
			}
		}
	}
}


