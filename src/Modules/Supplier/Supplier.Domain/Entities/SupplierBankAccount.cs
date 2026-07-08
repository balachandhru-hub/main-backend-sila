using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;
namespace Supplier.Domain.Entities
{
public class SupplierBankAccount : BaseModel
{
    
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string AccountHolderName { get; set; }

    public string BankName { get; set; }

    public string BranchName { get; set; }

    public string AccountNumber { get; set; }

    public string IFSCCode { get; set; }

    public string? SWIFTCode { get; set; }
    public string? IBAN { get; set; }
    public string Currency { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsVerified { get; set; }
    public SupplierBankAccount() { }
}
}
