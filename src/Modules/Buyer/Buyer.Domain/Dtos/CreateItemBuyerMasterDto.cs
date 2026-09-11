namespace Buyer.Domain.Dtos
{
    public class CreateItemBuyerMasterDto
    {
        public string Description { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialGroup { get; set; }
        public string ProductType { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string OrderUnitOfMeasure { get; set; }
        public string AlternateUnitOfMeasure { get; set; }
        public string ValuationClass { get; set; }
        public string UnitOfMeasureMapping { get; set; }
        public string? SubUnit { get; set; }
        public string? MicroUnit { get; set; }

        public Guid ApprovalFlowId { get; set; }

        public string? Comment { get; set; }
    }
}