using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class CreateSupplierCatalogDto
    {
        public string CatalogName { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string UnitOfMeasure { get; set; }

        public List<AssetUploadDto> Assets { get; set; } = new();
    }
}