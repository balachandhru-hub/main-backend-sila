using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Buyer.Domain.Common;
using Buyer.Infrastructure.Contracts.IRepository;
using HashingSystem;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.NotifyExternalSupplier
{
    public class NotifyExternalSupplierCommandHandler
        : IRequestHandler<NotifyExternalSupplierCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IAesEncryption _aesEncryption;
        private readonly ILoggerManager _logger;
        private readonly IMetadataApiClient _metadataApiClient;

        public NotifyExternalSupplierCommandHandler(
            IRepositoryWrapper repository,
            IAesEncryption aesEncryption,
            ILoggerManager logger,
            IMetadataApiClient metadataApiClient)
        {
            _repository = repository;
            _aesEncryption = aesEncryption;
            _logger = logger;
            _metadataApiClient = metadataApiClient;
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
                var registrationLink = BuildRegistrationLink(externalSupplier.Id, rfq.Id);

                var parameters = new Dictionary<string, string>
                {
                    { "SUPPLIER_NAME", externalSupplier.SupplierName ?? string.Empty },
                    { "RFQ_NUMBER", rfq.RFQNumber ?? string.Empty },
                    { "RFQ_TITLE", rfq.Title ?? string.Empty },
                    { "REGISTRATION_LINK", registrationLink }
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

        private string BuildRegistrationLink(Guid externalSupplierId, Guid rfqId)
        {
            var payload = JsonSerializer.Serialize(new
            {
                ExternalSupplierId = externalSupplierId,
                RFQId = rfqId
            });

            var token = _aesEncryption.Encrypt(payload);
            var encodedToken = Uri.EscapeDataString(token);

            var origin = Common.REGISTRATION_LINK;

            return $"{origin.TrimEnd('/')}/supplier/register?token={encodedToken}";
        }
    }
}
