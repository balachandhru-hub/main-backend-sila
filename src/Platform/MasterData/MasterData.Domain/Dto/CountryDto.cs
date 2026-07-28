namespace MasterData.Domain.Dto;

public class CountryDto
{
    public Guid Id { get; set; }
    public string CountryName { get; set; }
    public string CountryCode { get; set; }
    public string MobileCountryCode { get; set; }
}
