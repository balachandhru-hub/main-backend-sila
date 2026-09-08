using Microsoft.EntityFrameworkCore;

using SharedKernel.Contracts;
using SharedKernel.ExceptionHandler;

using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Services
{
    /// <summary>
    /// Validates an external-supplier session token against the
    /// <see cref="Supplier.Domain.Entities.SupplierRFQ"/> record it was
    /// issued for (RFQId, expiry = RFQ EndDate).
    /// </summary>
    public class SupplierSessionTokenValidator : ISessionTokenValidator
    {
        private readonly IRepositoryWrapper _repository;

        public SupplierSessionTokenValidator(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<SessionTokenValidationResult> ValidateAsync(
            string sessionToken,
            Guid rfqId,
            CancellationToken cancellationToken = default)
        {
            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x => x.SessionToken == sessionToken && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Invalid session token.");
            }

            if (supplierRFQ.BuyerRFQId != rfqId)
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Session token does not belong to this RFQ.");
            }

            if (DateTime.UtcNow > supplierRFQ.EndDate)
            {
                throw new ForBiddenCustomException(
                    "Forbidden",
                    "Session token has expired. The RFQ has reached its end date.");
            }

            return new SessionTokenValidationResult
            {
                RFQId = supplierRFQ.BuyerRFQId,
                SupplierId = supplierRFQ.SupplierId,
                SupplierRFQId = supplierRFQ.Id
            };
        }
    }
}
