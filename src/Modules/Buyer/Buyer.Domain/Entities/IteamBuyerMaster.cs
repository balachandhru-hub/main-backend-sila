using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{public class IteamBuyerMaster :BaseModel
{

[Key]
    public Guid Id { get; set; }
public Guid BuyerId{get;set;}
public string Description {get;set;}
public string MaterialCode{get;set;}
public string MaterialGroup{get;set;}
public IteamBuyerMaster(){}
}
}