namespace ImageSorting.API.Models
{
    public record BlobUploadResponse(int Uploaded, IReadOnlyList<string> Items);
}


