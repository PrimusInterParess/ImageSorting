using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImageSorting.Data.Entities
{
	public class FileMetadata
	{
		[Key]
		[ForeignKey(nameof(File))]
		public long FileId { get; set; }

		public DateTime? BestDateTakenUtc { get; set; }
		public string? BestDateSource { get; set; }
		public int? Width { get; set; }
		public int? Height { get; set; }
		public string? CameraMake { get; set; }
		public string? CameraModel { get; set; }
		public string? ContentType { get; set; }
		public string? Extension { get; set; }

		public File File { get; set; }
	}
}




