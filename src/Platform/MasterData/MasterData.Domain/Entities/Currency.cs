
using MasterData.Domain.Common;

namespace MasterData.Domain.Entities
{
    public class Currency : BaseEntity
    {
        public Guid Id { get; set; }
      
 
        public string? CurrencyName { get; set; }
        public int? SortNumber { get; set; }
         public Currency() 
          { }
 

    }
}