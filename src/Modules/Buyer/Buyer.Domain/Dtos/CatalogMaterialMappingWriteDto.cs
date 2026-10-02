namespace Buyer.Domain.Dtos
{
    public class CatalogMaterialMappingWriteDto
    {
        public Guid CatalogId { get; set; }

        /// <summary>
        /// Item Master id of the buyer.
        /// </summary>
        public Guid MaterialId { get; set; }
    }
}
