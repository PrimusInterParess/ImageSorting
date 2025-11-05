namespace ImageSorting.Core.Models
{
	public record BlobItemInfo(
		string Name,
		long? Size,
		string? ContentType,
		DateTimeOffset? LastModified
	);

	public record BlobListResponse(
		int Count,
		IReadOnlyList<BlobItemInfo> Items
	);

	public record ContainerListResponse(
		int Count,
		IReadOnlyList<string> Items
	);

	public record PrefixListResponse(
		int Count,
		IReadOnlyList<string> Items
	);
}


