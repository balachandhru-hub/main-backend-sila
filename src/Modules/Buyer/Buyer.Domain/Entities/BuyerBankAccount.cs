using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Entities
{
public class BuyerBankAccount : BaseModel
{
    [Key]
    [Required]
    public Guid Id { get; set; }

    public Guid BuyerId { get; set; }

    public string AccountHolderName { get; set; }

    public string BankName { get; set; }

    public string BranchName { get; set; }

    public string AccountNumber { get; set; }

    public string IFSCCode { get; set; }

    public string? SWIFTCode { get; set; }

    public string Currency { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsVerified { get; set; }
    public BuyerBankAccount()
    {
    }
}
}