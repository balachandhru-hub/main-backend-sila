using Microsoft.AspNetCore.Http;

namespace Buyer.Domain.Dto
{
    public class UploadDepartmentCostCenterDto
    {
        public Guid? BuyerId { get; set; }

        public Guid OrganizationId { get; set; }

        public IFormFile File { get; set; }
    }
}