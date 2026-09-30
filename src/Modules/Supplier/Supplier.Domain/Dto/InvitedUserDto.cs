namespace Supplier.Domain.Dto
{
    public class InvitedUserDto
    {
        public Guid RFQId { get; set; }

        public Guid SupplierId { get; set; }

        public Guid OrganizationId { get; set; }

        public Guid UserId { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? UserName { get; set; }
    }
}
