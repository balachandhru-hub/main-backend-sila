using System.Text.Json.Serialization;
using SharedKernel.Dto;

namespace Buyer.Domain.Dtos
{
    /// <summary>
    /// Common fields shared by every pending approval item, regardless of
    /// flow. The concrete type returned is always one of
    /// <see cref="ManualPendingApprovalDto"/> or
    /// <see cref="ExcelPendingApprovalDto"/> - never this base type
    /// directly - and STJ writes out only that concrete type's own
    /// fields (via UploadType as the polymorphic discriminator), so
    /// MANUAL and EXCEL items never carry fields that don't apply to them.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "uploadType")]
    [JsonDerivedType(typeof(ManualPendingApprovalDto), "MANUAL")]
    [JsonDerivedType(typeof(ExcelPendingApprovalDto), "EXCEL")]
    public abstract class PendingApprovalDto
    {
        public Guid PredefinedMaterialId { get; set; }

        public Guid ApprovalId { get; set; }

        public Guid ApprovalFlowPredefinedMaterialId { get; set; }

        public Guid ApprovalMappingId { get; set; }

        public int Order { get; set; }

        public string? ApprovalStatus { get; set; }

        public string? Status { get; set; }
    }

    /// <summary>Manual single-material approval (PredefinedMaterial).</summary>
    public class ManualPendingApprovalDto : PendingApprovalDto
    {
        public string? MaterialCode { get; set; }

        public string? ProductType { get; set; }

        public string? Description { get; set; }

        public string? MaterialGroup { get; set; }
    }

    /// <summary>Bulk Excel batch approval (ExcelMaterialMaster) - one approval for the whole uploaded file.</summary>
    public class ExcelPendingApprovalDto : PendingApprovalDto
    {
        public string? Title { get; set; }

        public AssetDto? Asset { get; set; }
    }
}
