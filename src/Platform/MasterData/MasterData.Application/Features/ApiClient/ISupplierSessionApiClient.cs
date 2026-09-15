namespace MasterData.Application.Contracts
{
    public interface ISupplierSessionApiClient
    {
        Task<bool> ValidateExternalSessionToken(
            string sessionToken,
            Guid rfqId,
            CancellationToken cancellationToken = default);
    }
}
