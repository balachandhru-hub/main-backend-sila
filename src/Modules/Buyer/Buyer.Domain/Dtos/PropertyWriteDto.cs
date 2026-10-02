namespace Buyer.Domain.Dtos
{
    public class PropertyWriteDto
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string PlantCode { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Approval flow (type WEEKLY_BUCKET) used when the weekly bucket of this property is frozen.
        /// </summary>
        public Guid? MasterApprovalFlowId { get; set; }
    }
}
