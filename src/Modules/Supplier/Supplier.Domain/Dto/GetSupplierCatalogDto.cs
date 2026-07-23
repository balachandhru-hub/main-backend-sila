using SharedKernel.Dto;

public class GetSupplierCatalogDto
{
    public Guid Id { get; set; }

    public string CatalogName { get; set; }

    public string Description { get; set; }

    public decimal Price { get; set; }

    public string UnitOfMeasure { get; set; }

    public List<AssetDto> Assets { get; set; } = new();
}