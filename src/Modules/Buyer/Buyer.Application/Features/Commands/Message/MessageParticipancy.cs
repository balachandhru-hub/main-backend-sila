using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateMessage
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
            Guid? requestedSupplierId,
            ILoggerManager logger)
        {
            bool isBuyer = string.Equals(organizationType, "Buyer", StringComparison.OrdinalIgnoreCase);
            bool isSupplier = string.Equals(organizationType, "Supplier", StringComparison.OrdinalIgnoreCase);

            if (isBuyer)
            {
                BuyerBusinessProfile? buyer = repository.BuyerBusinessProfile
                    .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

                if (buyer == null || buyer.Id != rfq.BuyerId)
                {
                    logger.LogError($"Forbidden: Buyer OrganizationId {organizationId} does not have access to RFQId {rfq.Id}.");
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
                }

                if (requestedSupplierId == null)
                {
                    logger.LogError($"Invalid request: SupplierId is required. RFQId: {rfq.Id}, OrganizationId: {organizationId}.");
                    throw new BadRequestCustomException("Invalid request", "SupplierId is required.");
                }

                RFQSupplierMapping? mapping = repository.RFQSupplierMapping
                    .FindFirstByCondition(x => x.RFQId == rfq.Id && x.SupplierId == requestedSupplierId && x.IsActive);

                if (mapping == null)
                {
                    logger.LogError($"Supplier not found: SupplierId {requestedSupplierId} is not invited to RFQId {rfq.Id}.");
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
                    logger.LogError($"Forbidden: Supplier OrganizationId {organizationId} does not have access to RFQId {rfq.Id}.");
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
                }

                return (rfq.BuyerId, orgMapping.SupplierId, false);
            }

            logger.LogError($"Forbidden: Unknown OrganizationType '{organizationType}' for RFQId {rfq.Id}.");
            throw new ForBiddenCustomException("Forbidden", "Unknown organization type.");
        }

        public static bool ResolveForThread(
            IRepositoryWrapper repository,
            MessageThread thread,
            Guid organizationId,
            string organizationType,
            ILoggerManager logger)
        {
            bool isBuyer = string.Equals(organizationType, "Buyer", StringComparison.OrdinalIgnoreCase);
            bool isSupplier = string.Equals(organizationType, "Supplier", StringComparison.OrdinalIgnoreCase);

            if (isBuyer)
            {
                BuyerBusinessProfile? buyer = repository.BuyerBusinessProfile
                    .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

                if (buyer == null || buyer.Id != thread.BuyerId)
                {
                    logger.LogError($"Forbidden: Buyer OrganizationId {organizationId} does not have access to ThreadId {thread.Id}.");
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
                    logger.LogError($"Forbidden: Supplier OrganizationId {organizationId} does not have access to ThreadId {thread.Id}.");
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
                }

                return false;
            }

            logger.LogError($"Forbidden: Unknown OrganizationType '{organizationType}' for ThreadId {thread.Id}.");
            throw new ForBiddenCustomException("Forbidden", "Unknown organization type.");
        }

        /// <summary>
        /// Resolves/validates a Buyer-JWT caller sending to a specific ExternalSupplier on
        /// this RFQ (the requested party must have an active RFQExternalSupplier invite).
        /// </summary>
        public static (Guid BuyerId, Guid ExternalSupplierId) ResolveForRFQAsBuyer(
            IRepositoryWrapper repository,
            RFQ rfq,
            Guid organizationId,
            Guid requestedExternalSupplierId,
            ILoggerManager logger)
        {
            BuyerBusinessProfile? buyer = repository.BuyerBusinessProfile
                .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

            if (buyer == null || buyer.Id != rfq.BuyerId)
            {
                logger.LogError($"Forbidden: Buyer OrganizationId {organizationId} does not have access to RFQId {rfq.Id}.");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
            }

            RFQExternalSupplier? mapping = repository.RFQExternalSupplier
                .FindFirstByCondition(x => x.RFQId == rfq.Id && x.ExternalSupplierId == requestedExternalSupplierId && x.IsActive);

            if (mapping == null)
            {
                logger.LogError($"External supplier not found: ExternalSupplierId {requestedExternalSupplierId} is not invited to RFQId {rfq.Id}.");
                throw new NotFoundCustomException("External supplier not found.", "External supplier is not invited to this RFQ.");
            }

            return (buyer.Id, requestedExternalSupplierId);
        }

        /// <summary>
        /// Resolves/validates an ExternalSupplier session-token caller (already authenticated
        /// against the Supplier microservice's session-token store) against this RFQ.
        /// </summary>
        public static Guid ResolveForRFQAsExternalSupplier(
            IRepositoryWrapper repository,
            RFQ rfq,
            Guid externalSupplierId,
            ILoggerManager logger)
        {
            RFQExternalSupplier? mapping = repository.RFQExternalSupplier
                .FindFirstByCondition(x => x.RFQId == rfq.Id && x.ExternalSupplierId == externalSupplierId && x.IsActive);

            if (mapping == null)
            {
                logger.LogError($"Forbidden: ExternalSupplierId {externalSupplierId} does not have access to RFQId {rfq.Id}.");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
            }

            return rfq.BuyerId;
        }

        /// <summary>Buyer-JWT-caller variant of <see cref="ResolveForThread"/> for an ExternalSupplier thread.</summary>
        public static void ResolveExternalThreadForBuyer(
            IRepositoryWrapper repository,
            MessageThread thread,
            Guid organizationId,
            ILoggerManager logger)
        {
            BuyerBusinessProfile? buyer = repository.BuyerBusinessProfile
                .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

            if (buyer == null || buyer.Id != thread.BuyerId)
            {
                logger.LogError($"Forbidden: Buyer OrganizationId {organizationId} does not have access to ThreadId {thread.Id}.");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
            }
        }

        /// <summary>Session-token-caller variant of <see cref="ResolveForThread"/> for an ExternalSupplier thread.</summary>
        public static void ResolveExternalThreadForExternalSupplier(
            MessageThread thread,
            Guid externalSupplierId,
            ILoggerManager logger)
        {
            if (thread.ExternalSupplierId != externalSupplierId)
            {
                logger.LogError($"Forbidden: ExternalSupplierId {externalSupplierId} does not have access to ThreadId {thread.Id}.");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
            }
        }
    }
}
