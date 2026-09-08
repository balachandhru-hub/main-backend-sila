using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Buyer.Domain.Common;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.NotifyExternalSupplier
{
    public class NotifyExternalSupplierCommandHandler
        : IRequestHandler<NotifyExternalSupplierCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMetadataApiClient _metadataApiClient;
        private readonly IConfiguration _configuration;

        public NotifyExternalSupplierCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMetadataApiClient metadataApiClient,
            IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _metadataApiClient = metadataApiClient;
            _configuration = configuration;
        }

        public async Task<bool> Handle(
            NotifyExternalSupplierCommand request,
            CancellationToken cancellationToken)
        {
            var externalSupplier = await _repository.ExternalSupplier
                .FindByCondition(x => x.Id == request.ExternalSupplierId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            // Not an external supplier - nothing to do.
            if (externalSupplier == null)
            {
                return false;
            }

            var mapping = await _repository.RFQExternalSupplier
                .FindByCondition(x =>
                    x.RFQId == request.BuyerRFQId &&
                    x.ExternalSupplierId == externalSupplier.Id &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (mapping == null)
            {
                _logger.LogError(
                    $"RFQ/ExternalSupplier mapping not found. RFQId: {request.BuyerRFQId}, ExternalSupplierId: {externalSupplier.Id}");
                throw new NotFoundCustomException(
                    "RFQ/ExternalSupplier mapping not found.",
                    $"No mapping found for RFQId: {request.BuyerRFQId} and ExternalSupplierId: {externalSupplier.Id}");
            }

            var rfq = await _repository.RFQ
                .FindByCondition(x => x.Id == request.BuyerRFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. RFQId: {request.BuyerRFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {request.BuyerRFQId}");
            }

            try
            {
                var bidLink = BuildBidLink(rfq.Id, request.SessionToken);

                var parameters = new Dictionary<string, string>
                {
                    { "SUPPLIER_NAME", externalSupplier.SupplierName ?? string.Empty },
                    { "RFQ_NUMBER", rfq.RFQNumber ?? string.Empty },
                    { "RFQ_TITLE", rfq.Title ?? string.Empty },
                    // The invite email's "Submit Your Quotation" button uses the
                    // REGISTRATION_LINK placeholder; for this pre-bid invite it must
                    // point at the bid page, not supplier registration.
                    { "REGISTRATION_LINK", bidLink }
                };

                await _metadataApiClient.SendEmailAsync(
                    externalSupplier.Email,
                    Common.EXTERNAL_SUPPLIER_EMAIL_KEY,
                    rfq.Id,
                    Common.EXTERNAL_SUPPLIER_ENTITY_TYPE,
                    parameters,
                    cancellationToken);

                _logger.LogInfo(
                    $"External supplier quotation invite email sent. ExternalSupplierId: {externalSupplier.Id}, RFQId: {rfq.Id}");
            }
            catch (Exception ex)
            {
                // A mail-server hiccup must not fail an already-successful
                // RFQ creation - log and continue.
                _logger.LogError(
                    $"Failed to send external supplier quotation invite email. ExternalSupplierId: {externalSupplier.Id}, RFQId: {rfq.Id}. Error: {ex.Message}");
            }

            return true;
        }

        private string BuildBidLink(Guid rfqId, string sessionToken)
        {
            var origin = _configuration[Common.EXTERNAL_SUPPLIER_BID_LINK]!;

            return $"{origin.TrimEnd('/')}/external-supplier/bid/{rfqId}/{Uri.EscapeDataString(sessionToken)}";
        }
    }
}
