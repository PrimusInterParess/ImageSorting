using ImageSorting.API.Models;
using Microsoft.AspNetCore.Http;

namespace ImageSorting.API.Services
{
	public interface IUploadOrchestrationService
	{
		Task<IReadOnlyList<BlobUploadItemWithMetadata>> UploadAndPersistAsync(
			string container,
			string? prefix,
			IFormFileCollection files,
			CancellationToken ct);
	}
}


