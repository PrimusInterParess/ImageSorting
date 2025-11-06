using ImageSorting.API.Models;
using ImageSorting.Core.Interfaces;
using ImageSorting.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageSorting.API.Controllers
{
    [ApiController]
    [Route("api/upload")] 
    public class UploadController : ControllerBase
    {
		private readonly IBlobUploadService _uploadService;
		private readonly IFileMetadataService _metadataService;
		private readonly IUploadOrchestrationService _orchestrationService;

		public UploadController(IBlobUploadService uploadService, IFileMetadataService metadataService, IUploadOrchestrationService orchestrationService)
        {
			_uploadService = uploadService;
			_metadataService = metadataService;
			_orchestrationService = orchestrationService;
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

        [HttpPost("blob-with-metadata")]
        [RequestSizeLimit(524288000)] // 500 MB cap; adjust as needed
        public async Task<ActionResult<BlobUploadWithMetadataResponse>> UploadToBlobWithMetadata([FromForm] string container, [FromForm] string? prefix, [FromForm] IFormFileCollection files, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(container))
            {
                return BadRequest("container is required");
            }
            if (files == null || files.Count == 0)
            {
                return BadRequest("no files provided");
            }

			var paired = await _orchestrationService.UploadAndPersistAsync(container, prefix, files, ct);
			return Ok(new BlobUploadWithMetadataResponse(paired.Count, paired));
        }
    }
}


