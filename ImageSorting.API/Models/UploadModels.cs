using ImageSorting.Core.Models;

namespace ImageSorting.API.Models
{
	public record BlobUploadResponse(int Uploaded, IReadOnlyList<string> Items);

	public record BlobUploadItemWithMetadata(string BlobName, FileMetadataDto Metadata);

	public record BlobUploadWithMetadataResponse(int Uploaded, IReadOnlyList<BlobUploadItemWithMetadata> Items);
}



