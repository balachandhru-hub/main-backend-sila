using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Application.Contracts
{
    public interface ISupplierIdApiClient
    {
        Task<Guid?> GetSupplierId(
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}