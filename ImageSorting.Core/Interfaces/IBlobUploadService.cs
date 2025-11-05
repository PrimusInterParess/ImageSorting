using Microsoft.AspNetCore.Http;

namespace ImageSorting.Core.Interfaces
{
	public interface IBlobUploadService
	{
		Task<IReadOnlyList<string>> UploadAsync(string container, string? prefix, IFormFileCollection files, CancellationToken ct);
	}
}


