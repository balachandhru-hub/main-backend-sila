using Microsoft.AspNetCore.Http;

namespace Supplier.Domain.Dto
{
    public class SupplierAssetUploadDto
    {
        public IFormFile File { get; set; }
    }
}