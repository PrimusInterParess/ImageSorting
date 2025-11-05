using ImageSorting.Core.Models;

namespace ImageSorting.Core.Interfaces
{
	public interface IBlobImageSortingService
	{
		Task<BlobSortResult> SortAsync(BlobSortRequest request, CancellationToken cancellationToken);
	}
}


