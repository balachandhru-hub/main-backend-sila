using Buyer.Domain.Dto;

public interface IMetadataApiClient
{
    Task<List<MetadataDto>?> GetReferenceList(List<string> key);
    Task<string> GetRefTermKeyById(Guid id);
   
}