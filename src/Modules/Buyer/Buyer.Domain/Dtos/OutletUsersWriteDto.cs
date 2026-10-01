namespace Buyer.Domain.Dtos
{
    public class OutletUsersWriteDto
    {
        /// <summary>
        /// The outlets the user works with. Replaces the outlets currently assigned to the user.
        /// </summary>
        public List<Guid> OutletIds { get; set; } = new();
    }
}
