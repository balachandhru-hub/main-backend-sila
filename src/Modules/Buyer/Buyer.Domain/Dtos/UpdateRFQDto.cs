namespace Buyer.Domain.Dto
{
    public class UpdateRFQDto
    {
        public string? Description { get; set; }

        public string? Department { get; set; }

        public string? Region { get; set; }

        public string? DeliveryLocation { get; set; }

        public DateTime DeliveryTargetDate { get; set; }

        public decimal Budget { get; set; }

        public bool AddLotOption { get; set; }

        public List<UpdateRFQItemDto> Items { get; set; } = new();

        public List<UpdateRFQQuestionDto> Questions { get; set; } = new();

        public List<Guid> SupplierIds { get; set; } = new();
    }


    public class UpdateRFQItemDto
    {
        public Guid? Id { get; set; }

        public string? Description { get; set; }

        public decimal Quantity { get; set; }

        public string? UOM { get; set; }

        public string? MaterialCode { get; set; }

        public string? MaterialGroup { get; set; }

        public string? CostCenter { get; set; }
    }


    public class UpdateRFQQuestionDto
    {
        public Guid? Id { get; set; }

        public string? Question { get; set; }

        public string? QuestionType { get; set; }

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }

        public List<UpdateRFQQuestionOptionDto> Options { get; set; } = new();
    }


    public class UpdateRFQQuestionOptionDto
    {
        public Guid? Id { get; set; }

        public string? OptionText { get; set; }

        public int DisplayOrder { get; set; }
    }
}