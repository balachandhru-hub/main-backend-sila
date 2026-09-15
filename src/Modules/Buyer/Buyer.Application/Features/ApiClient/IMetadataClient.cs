using Buyer.Domain.Dto;

public interface IMetadataApiClient
{
    Task<List<MetadataDto>?> GetReferenceList(List<string> key);
    Task<string> GetRefTermKeyById(Guid id);
    Task<string> GetExternalRefTermKeyById(Guid id, string sessionToken, Guid rfqId);
    Task SendEmailAsync(
        string toEmail,
        string emailKey,
        Guid? entityId,
        string? entityType,
        Dictionary<string, string>? parameters,
        CancellationToken cancellationToken = default);

}