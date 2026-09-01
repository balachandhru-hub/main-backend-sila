namespace Supplier.Domain.Dto
{
   
public class SaveSupplierRFQAnswerDto
{
    public Guid SupplierRFQId { get; set; }
    public Guid SupplierId { get; set; }

    public List<SaveSupplierRFQQuestionAnswerDto> Answers { get; set; } 
}
}