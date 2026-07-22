using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Application.Contracts
{
    public interface IBuyerIdApiClient
    {
        Task<Guid> GetBuyerId(
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}