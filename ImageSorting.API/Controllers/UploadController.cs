using ImageSorting.API.Models;
using ImageSorting.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ImageSorting.API.Controllers
{
    [ApiController]
    [Route("api/upload")] 
    public class UploadController : ControllerBase
    {
        private readonly IBlobUploadService _uploadService;

        public UploadController(IBlobUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpPost("blob")]
        [RequestSizeLimit(524288000)] // 500 MB cap; adjust as needed
        public async Task<ActionResult<BlobUploadResponse>> UploadToBlob([FromForm] string container, [FromForm] string? prefix, [FromForm] IFormFileCollection files, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(container))
            {
                return BadRequest("container is required");
            }
            if (files == null || files.Count == 0)
            {
                return BadRequest("no files provided");
            }

            var items = await _uploadService.UploadAsync(container, prefix, files, ct);
            return Ok(new BlobUploadResponse(items.Count, items));
        }
    }
}


