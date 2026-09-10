using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.Message
{
    /// <summary>
    /// Resolves and validates which Buyer/Supplier a caller may act as for a given RFQ or
    /// message thread. Centralized here because the same access rule is enforced by every
    /// message command/query handler.
    /// </summary>
    internal static class MessageParticipancy
    {
        public static (Guid BuyerId, Guid SupplierId, bool IsBuyer) ResolveForRFQ(
            IRepositoryWrapper repository,
            RFQ rfq,
            Guid organizationId,
            string organizationType,
            Guid? requestedSupplierId)
        {
            bool isBuyer = string.Equals(organizationType, "Buyer", StringComparison.OrdinalIgnoreCase);
            bool isSupplier = string.Equals(organizationType, "Supplier", StringComparison.OrdinalIgnoreCase);

            if (isBuyer)
            {
                BuyerBusinessProfile? buyer = repository.BuyerBusinessProfile
                    .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

                if (buyer == null || buyer.Id != rfq.BuyerId)
                {
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
                }

                if (requestedSupplierId == null)
                {
                    throw new BadRequestCustomException("Invalid request", "SupplierId is required.");
                }

                RFQSupplierMapping? mapping = repository.RFQSupplierMapping
                    .FindFirstByCondition(x => x.RFQId == rfq.Id && x.SupplierId == requestedSupplierId && x.IsActive);

                if (mapping == null)
                {
                    throw new NotFoundCustomException("Supplier not found.", "Supplier is not invited to this RFQ.");
                }

                return (buyer.Id, requestedSupplierId.Value, true);
            }

            if (isSupplier)
            {
                RFQOrganizationUserMapping? orgMapping = repository.RFQOrganizationUserMapping
                    .FindFirstByCondition(x => x.RFQId == rfq.Id && x.OrganizationId == organizationId && x.IsActive);

                if (orgMapping == null)
                {
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
                }

                return (rfq.BuyerId, orgMapping.SupplierId, false);
            }

            throw new ForBiddenCustomException("Forbidden", "Unknown organization type.");
        }

        public static bool ResolveForThread(
            IRepositoryWrapper repository,
            MessageThread thread,
            Guid organizationId,
            string organizationType)
        {
            bool isBuyer = string.Equals(organizationType, "Buyer", StringComparison.OrdinalIgnoreCase);
            bool isSupplier = string.Equals(organizationType, "Supplier", StringComparison.OrdinalIgnoreCase);

            if (isBuyer)
            {
                BuyerBusinessProfile? buyer = repository.BuyerBusinessProfile
                    .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

                if (buyer == null || buyer.Id != thread.BuyerId)
                {
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
                }

                return true;
            }

            if (isSupplier)
            {
                RFQOrganizationUserMapping? orgMapping = repository.RFQOrganizationUserMapping
                    .FindFirstByCondition(x =>
                        x.RFQId == thread.RFQId &&
                        x.OrganizationId == organizationId &&
                        x.SupplierId == thread.SupplierId &&
                        x.IsActive);

                if (orgMapping == null)
                {
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
                }

                return false;
            }

            throw new ForBiddenCustomException("Forbidden", "Unknown organization type.");
        }
    }
}
