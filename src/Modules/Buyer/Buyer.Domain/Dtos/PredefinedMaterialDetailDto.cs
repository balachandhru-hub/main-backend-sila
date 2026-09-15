namespace Buyer.Domain.Dtos
{
    public class PredefinedMaterialDetailDto
    {
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public string? BaseUnitOfMeasure { get; set; }

        public string? OrderUnitOfMeasure { get; set; }

        public string? AlternateUnitOfMeasure { get; set; }

        public string? ValuationClass { get; set; }

        public string? UnitOfMeasureMapping { get; set; }

        public string? SubUnit { get; set; }

        public string? MicroUnit { get; set; }

        public string? Status { get; set; }

        public List<Guid> ApprovalUserIds { get; set; } = new();
    }
}
