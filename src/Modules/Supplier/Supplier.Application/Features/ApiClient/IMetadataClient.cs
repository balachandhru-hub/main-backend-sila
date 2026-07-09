using Supplier.Domain.Dto;

public interface IMetadataApiClient
{
    Task<List<MetadataDto>?> GetReferenceList(List<string> key);
}