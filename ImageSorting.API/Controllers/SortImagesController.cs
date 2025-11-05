using ImageSorting.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ImageSorting.API.Controllers
{
    [ApiController]
    [Route("api/sort-images")]
    public class SortImagesController : ControllerBase
    {
        private readonly IImageSortingService _imageSortingService;

        public SortImagesController(IImageSortingService imageSortingService)
        {
            _imageSortingService = imageSortingService;
        }

        [HttpPost]
        public async Task<IActionResult> Sort([FromBody] SortRequest request, CancellationToken cancellationToken)
        {
            var result = await _imageSortingService.SortAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}


