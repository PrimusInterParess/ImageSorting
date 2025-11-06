namespace ImageSorting.Core.Models
{
	public record FileMetadataDto(
		DateTime? BestDateTakenUtc,
		string? BestDateSource,
		int? Width,
		int? Height,
		string? CameraMake,
		string? CameraModel,
		string? ContentType,
		string? Extension
	);
}


