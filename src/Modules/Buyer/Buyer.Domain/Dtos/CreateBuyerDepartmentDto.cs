

namespace Buyer.Domain.Dto
{
    public class CreateBuyerDepartmentDto
    {
        public Guid OrganizationId { get; set; }

        public string Department { get; set; }
        public Guid? BuyerId { get; set; }
        public List<string> CostCenter { get; set; }
    }
    public class CreateBuyerCostCenterDto
    {
        public Guid DepartmentId { get; set; }

        public string CostCenter { get; set; }
    }
}