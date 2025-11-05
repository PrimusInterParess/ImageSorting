using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageSorting.API.Models;

namespace ImageSorting.API.Services
{
	public interface IBlobBrowseService
	{
		Task<IReadOnlyList<string>> ListContainersAsync(CancellationToken ct);
		Task<IReadOnlyList<string>> ListPrefixesAsync(string container, string? prefix, CancellationToken ct);
		Task<BlobListResponse> ListAsync(string container, string? prefix, CancellationToken ct);
	}

	public class BlobBrowseService : IBlobBrowseService
	{
		private readonly BlobServiceClient _blobServiceClient;

		public BlobBrowseService(BlobServiceClient blobServiceClient)
		{
			_blobServiceClient = blobServiceClient;
		}

		public async Task<IReadOnlyList<string>> ListContainersAsync(CancellationToken ct)
		{
			var containers = new List<string>();
			await foreach (var containerItem in _blobServiceClient.GetBlobContainersAsync(cancellationToken: ct))
			{
				containers.Add(containerItem.Name);
			}
			containers.Sort(StringComparer.Ordinal);
			return containers;
		}

		public async Task<IReadOnlyList<string>> ListPrefixesAsync(string container, string? prefix, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(container))
			{
				throw new ArgumentException("Container is required");
			}

			var containerClient = _blobServiceClient.GetBlobContainerClient(container);
			var prefixes = new HashSet<string>(StringComparer.Ordinal);
			await foreach (var item in containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/", cancellationToken: ct))
			{
				if (item.IsPrefix && !string.IsNullOrWhiteSpace(item.Prefix))
				{
					var p = item.Prefix.TrimEnd('/');
					prefixes.Add(p);
				}
			}

			var list = new List<string>(prefixes);
			list.Sort(StringComparer.Ordinal);
			return list;
		}

		public async Task<BlobListResponse> ListAsync(string container, string? prefix, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(container))
			{
				throw new ArgumentException("Container is required");
			}

			var containerClient = _blobServiceClient.GetBlobContainerClient(container);
			var items = new List<BlobItemInfo>();

			await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: ct))
			{
				// Skip virtual folders
				if (blob.Properties.ContentLength == 0 && blob.Name.EndsWith("/", StringComparison.Ordinal))
				{
					continue;
				}

				items.Add(new BlobItemInfo(
					Name: blob.Name,
					Size: blob.Properties.ContentLength,
					ContentType: blob.Properties.ContentType,
					LastModified: blob.Properties.LastModified
				));
			}

			// Sort for stable output
			items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

			return new BlobListResponse(items.Count, items);
		}
	}
}



