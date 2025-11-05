using ImageSorting.Core.Models;
using ImageSorting.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ImageSorting.API.Controllers
{
	[ApiController]
	[Route("api/blob")]
	public class BlobBrowseController : ControllerBase
	{
		private readonly IBlobBrowseService _browseService;

		public BlobBrowseController(IBlobBrowseService browseService)
		{
			_browseService = browseService;
		}

		[HttpGet("list")]
		public async Task<ActionResult<BlobListResponse>> ListAsync([FromQuery] string container, [FromQuery] string? prefix, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(container))
			{
				return BadRequest("container is required");
			}

			var result = await _browseService.ListAsync(container, prefix, ct);
			return Ok(result);
		}

		[HttpGet("containers")]
		public async Task<ActionResult<ContainerListResponse>> ListContainersAsync(CancellationToken ct)
		{
			var items = await _browseService.ListContainersAsync(ct);
			return Ok(new ContainerListResponse(items.Count, items));
		}

		[HttpGet("prefixes")]
		public async Task<ActionResult<PrefixListResponse>> ListPrefixesAsync([FromQuery] string container, [FromQuery] string? prefix, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(container))
			{
				return BadRequest("container is required");
			}

			var items = await _browseService.ListPrefixesAsync(container, prefix, ct);
			return Ok(new PrefixListResponse(items.Count, items));
		}

		[HttpGet("prefixes/all")]
		public async Task<ActionResult<PrefixListResponse>> ListAllPrefixesAsync([FromQuery] string container, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(container))
			{
				return BadRequest("container is required");
			}

			var items = await _browseService.ListAllPrefixesAsync(container, ct);
			return Ok(new PrefixListResponse(items.Count, items));
		}
	}
}



