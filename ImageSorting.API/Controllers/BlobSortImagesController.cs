using ImageSorting.API.Models;
using ImageSorting.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageSorting.API.Controllers
{
    [ApiController]
    [Route("api/sort-images/blob")]
    public class BlobSortImagesController : ControllerBase
    {
        private readonly IBlobImageSortingService _service;

        public BlobSortImagesController(IBlobImageSortingService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<BlobSortResult>> SortAsync([FromBody] BlobSortRequest request, CancellationToken ct)
        {
            var result = await _service.SortAsync(request, ct);
            return Ok(result);
        }
    }
}


