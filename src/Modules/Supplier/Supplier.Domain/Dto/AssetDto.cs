

namespace Supplier.Domain.Dto
{
    public class AssetDto
    {
        public Guid Id { get; set; }

        public string? AssetType { get; set; }

        public string? AssetName { get; set; }

        public string FileType { get; set; }
        public string? FileName { get; set; }

    }
}