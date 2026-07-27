using MasterData.Domain.Common;

namespace MasterData.Domain.Entities
{

    public class CountryList: BaseEntity
    {
        public Guid Id { get; set; }

        public string CountryName{ get; set; }
        public string CountryCode{ get; set; }
        public string MobileCountryCode { get; set; }


        public CountryList()
        {}
    }

}