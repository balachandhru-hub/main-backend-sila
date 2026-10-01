namespace Buyer.Domain.Dtos
{
    public class OutletUserMappingDto
    {
        public Guid UserId { get; set; }
        public List<Guid> OutletIds { get; set; } = new();
    }
}
