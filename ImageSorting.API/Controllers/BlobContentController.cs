using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace ImageSorting.API.Controllers
{
	[ApiController]
	[Route("api/blob")] 
	public class BlobContentController : ControllerBase
	{
		private readonly BlobServiceClient _blobServiceClient;

		public BlobContentController(BlobServiceClient blobServiceClient)
		{
			_blobServiceClient = blobServiceClient;
		}

		[HttpGet("content")] 
		public async Task<IActionResult> GetContent([FromQuery] string container, [FromQuery] string name, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(container) || string.IsNullOrWhiteSpace(name))
			{
				return BadRequest("container and name are required");
			}

			var containerClient = _blobServiceClient.GetBlobContainerClient(container);
			var blobClient = containerClient.GetBlobClient(name);
			try
			{
				var download = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
				var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
				return File(download.Value.Content, contentType);
			}
			catch (RequestFailedException ex) when (ex.Status == 404)
			{
				return NotFound();
			}
		}
	}
}


