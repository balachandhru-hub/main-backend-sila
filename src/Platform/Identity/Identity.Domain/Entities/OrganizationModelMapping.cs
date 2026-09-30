
using SharedKernel.Models;
namespace Identity.Domain.Entities
{
    public class OrganizationModelMapping : BaseModel
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public Guid ModelId { get; set; }

        public OrganizationModelMapping()
        {
        }
    }
}