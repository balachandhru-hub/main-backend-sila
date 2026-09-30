using System.Text.Json.Serialization;
using SharedKernel.Dto;

namespace Buyer.Domain.Dtos
{
    /// <summary>
    /// Common fields shared by both flows. The concrete type returned is
    /// always <see cref="ManualPredefinedMaterialDetailDto"/> or
    /// <see cref="ExcelPredefinedMaterialDetailDto"/> - never this base
    /// type directly - so MANUAL and EXCEL items never carry fields that
    /// don't apply to them.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "uploadType")]
    [JsonDerivedType(typeof(ManualPredefinedMaterialDetailDto), "MANUAL")]
    [JsonDerivedType(typeof(ExcelPredefinedMaterialDetailDto), "EXCEL")]
    public abstract class PredefinedMaterialDetailDto
    {
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public string? Status { get; set; }

        public List<PredefinedMaterialApprovalUserDto> ApprovalUsers { get; set; } = new();
    }

    /// <summary>Manual single-material approval (PredefinedMaterial).</summary>
    public class ManualPredefinedMaterialDetailDto : PredefinedMaterialDetailDto
    {
        public string? BaseUnitOfMeasure { get; set; }

        public string? OrderUnitOfMeasure { get; set; }

        public string? AlternateUnitOfMeasure { get; set; }

        public string? ValuationClass { get; set; }

        public string? UnitOfMeasureMapping { get; set; }

        public string? SubUnit { get; set; }

        public string? MicroUnit { get; set; }
    }

    /// <summary>Bulk Excel batch approval (ExcelMaterialMaster) - one approval for the whole uploaded file.</summary>
    public class ExcelPredefinedMaterialDetailDto : PredefinedMaterialDetailDto
    {
        public string? Title { get; set; }

        public AssetDto? Asset { get; set; }
    }

    public class PredefinedMaterialApprovalUserDto
    {
        public Guid UserId { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public int Order { get; set; }

        public string? Status { get; set; }
    }
}
