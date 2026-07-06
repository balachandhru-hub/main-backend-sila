using MasterData.Domain.Common;

namespace MasterData.Domain.Entities
{
    public class EmailCCList : BaseEntity
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; }
        public string Email { get; set; }

        public EmailCCList()
        { }
    }
}