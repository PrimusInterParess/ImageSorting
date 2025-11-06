using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImageSorting.Data.Entities
{
    public class File
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Guid PublicId { get; set; }
        public string Name { get; set; }
        public long? Size { get; set; }
        public string BlobContainerName { get; set; }
        public string Prefix { get; set; }
        public string Exctention { get; set; }
        public DateTime DateTaken { get; set; }
        public DateTime DateUploaded { get; set; }
        public DateTime DateModified { get; set; }
        public FileMetadata? Metadata { get; set; }
    }
}
