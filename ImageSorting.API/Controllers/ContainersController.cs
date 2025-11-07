using Azure;
using Azure.Storage.Blobs;
using ImageSorting.Data;
using ImageSorting.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageSorting.API.Controllers
{
    [ApiController]
    [Route("api/containers")] 
    public class ContainersController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ImageSortingContext _db;

        public ContainersController(BlobServiceClient blobServiceClient, ImageSortingContext db)
        {
            _blobServiceClient = blobServiceClient;
            _db = db;
        }

        public record ContainerDto(long Id, string Name, DateTime DateCreatedUtc);
        public record CreateContainerRequest(string Name);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContainerDto>>> ListAsync(CancellationToken ct)
        {
            var items = await _db.Containers
                .OrderBy(c => c.Name)
                .Select(c => new ContainerDto(c.Id, c.Name, c.DateCreatedUtc))
                .ToListAsync(ct);
            return Ok(items);
        }

        [HttpPost]
        public async Task<ActionResult<ContainerDto>> CreateAsync([FromBody] CreateContainerRequest request, CancellationToken ct)
        {
            var name = (request?.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name is required");
            }

            if (!IsValidContainerName(name))
            {
                return BadRequest("Invalid container name. Use 3-63 lowercase letters, numbers, and hyphens; must start/end with letter or number.");
            }

            // Ensure it exists in Azure
            var containerClient = _blobServiceClient.GetBlobContainerClient(name);
            try
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
            }
            catch (RequestFailedException ex)
            {
                return Problem(detail: ex.Message, statusCode: ex.Status);
            }

            // Persist in DB if not exists
            var existing = await _db.Containers.SingleOrDefaultAsync(c => c.Name == name, ct);
            if (existing != null)
            {
                return Conflict(new { message = "Container already exists" });
            }

            var entity = new Container
            {
                Name = name,
                DateCreatedUtc = DateTime.UtcNow
            };
            _db.Containers.Add(entity);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(ListAsync), new { id = entity.Id }, new ContainerDto(entity.Id, entity.Name, entity.DateCreatedUtc));
        }

        private static bool IsValidContainerName(string name)
        {
            if (name.Length < 3 || name.Length > 63) return false;
            if (name.Any(c => !(c == '-' || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))) return false;
            if (!(char.IsLetterOrDigit(name[0]) && char.IsLetterOrDigit(name[^1]))) return false;
            if (name.Contains("--")) return false;
            return true;
        }
    }
}



