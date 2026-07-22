using Microsoft.AspNetCore.Http;

namespace Buyer.Domain.Dtos
{
    public class UploadItemBuyerMasterDto
    {
        public IFormFile File { get; set; } = default!;

        public Guid OrganizationId { get; set; }

        public Guid? BuyerId { get; set; }
    }
}