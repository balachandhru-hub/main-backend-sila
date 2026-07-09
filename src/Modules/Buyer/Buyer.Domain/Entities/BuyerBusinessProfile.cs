using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Entities
{
public class BuyerBusinessProfile : BaseModel
{
    [Key]
    [Required]
    public Guid Id { get; set; }

    // Reference to Identity Organization
    public Guid OrganizationId { get; set; }

    // Company Information
    public string OrganizationName { get; set; }

  

    public string Email { get; set; }

    public string Phone { get; set; }

    public bool EmailVerified { get; set; }

    // Address
    public string Country { get; set; }

    public string AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string PinCode { get; set; }

    // Business Information
    public string Industry { get; set; }

    // Manufacturing / Trading / Service
    public string BusinessType { get; set; }

    public int? EmployeeCount { get; set; }

    public decimal? AnnualTurnover { get; set; }

    public string Currency { get; set; }

    public int? YearEstablished { get; set; }

    public string? Website { get; set; }

    public string? Description { get; set; }
    public BuyerBusinessProfile()
    {
    }

   
}
}