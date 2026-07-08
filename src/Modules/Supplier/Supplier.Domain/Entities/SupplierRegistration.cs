using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Supplier.Domain.Entities
{
public class SupplierRegistration : BaseModel
{
    
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public Guid RegistrationType { get; set; }

    public string RegistrationNumber { get; set; }
   
 public string? RegistrationName { get; set; }

    public Guid? AssetId { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedOn { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public SupplierRegistration() { }
}
}