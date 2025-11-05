using ImageSorting.Core.Models;

namespace ImageSorting.Core.Interfaces
{
	public interface IBlobBrowseService
	{
		Task<IReadOnlyList<string>> ListContainersAsync(CancellationToken ct);
		Task<IReadOnlyList<string>> ListPrefixesAsync(string container, string? prefix, CancellationToken ct);
		Task<BlobListResponse> ListAsync(string container, string? prefix, CancellationToken ct);
	}
}


