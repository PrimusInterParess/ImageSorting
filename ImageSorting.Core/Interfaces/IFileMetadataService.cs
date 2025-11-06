using Microsoft.AspNetCore.Http;
using ImageSorting.Core.Models;

namespace ImageSorting.Core.Interfaces
{
    public interface IFileMetadataService
    {
        Task<FileMetadataDto> ExtractAsync(IFormFile file, CancellationToken ct);
        Task<IReadOnlyList<FileMetadataDto>> ExtractAsync(IFormFileCollection files, CancellationToken ct);
    }
}


