
namespace Buyer.Domain.Dto
{
 public class BuyerDepartmentDto
{
    public Guid Id { get; set; }

    public string Department { get; set; }
}
public class BuyerCostCenterDto
{
    public Guid Id { get; set; }

    public Guid DepartmentId { get; set; }

    public string CostCenter { get; set; }
}
}