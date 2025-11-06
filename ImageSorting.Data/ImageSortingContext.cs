using ImageSorting.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImageSorting.Data
{
    public class ImageSortingContext : DbContext
    {
        public ImageSortingContext(DbContextOptions<ImageSortingContext> options) : base(options) { }
        public DbSet<Entities.File> Files { get; set; }
        public DbSet<FileMetadata> FileMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var image = modelBuilder.Entity<Entities.File>();
            var metadata = modelBuilder.Entity<FileMetadata>();

            image.HasKey(x => x.Id).IsClustered();
            image.Property(x => x.Id).ValueGeneratedOnAdd();

            image.Property(x => x.PublicId)
                .HasDefaultValueSql("NEWSEQUENTIALID()")
                .ValueGeneratedOnAdd();

            image.HasIndex(x => x.PublicId)
                .IsUnique()
                .IsClustered(false);

            metadata.HasKey(x => x.FileId);

            image.HasOne(x => x.Metadata)
                .WithOne(x => x.File)
                .HasForeignKey<FileMetadata>(x => x.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
