using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class UpdateSupplierCatalogDto
    {
        public Guid Id { get; set; }

        public string? CatalogName { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }

        public string? UnitOfMeasure { get; set; }

        public List<AssetUploadDto>? Assets { get; set; }
    }
}