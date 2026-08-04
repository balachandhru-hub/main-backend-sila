using Microsoft.AspNetCore.Http;

namespace Supplier.Domain.Dto
{
    public class UploadSupplierCatalogDto
    {
        public IFormFile File { get; set; } = default!;

        public Guid OrganizationId { get; set; }
    }
}
