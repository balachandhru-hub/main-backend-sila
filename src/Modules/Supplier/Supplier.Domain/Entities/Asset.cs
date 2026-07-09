using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;
namespace Supplier.Domain.Entities
{
    public class Asset : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public Guid? EntityType { get; set; }

        public Guid? AssetType { get; set; }

        public string FileName { get; set; }

        public Guid? EntityId { get; set; }

        public Guid FileType { get; set; }

        public string? AssetName { get; set; }

        public Asset()
        {}
    }
}