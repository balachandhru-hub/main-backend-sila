using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using HashingSystem;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.NotifySupplierAwarded
{
    public class NotifySupplierAwardedCommandHandler
        : IRequestHandler<NotifySupplierAwardedCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IAesEncryption _aesEncryption;
        private readonly ILoggerManager _logger;
        private readonly IMetadataApiClient _metadataApiClient;
        private readonly IConfiguration _configuration;

        public NotifySupplierAwardedCommandHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            IAesEncryption aesEncryption,
            ILoggerManager logger,
            IMetadataApiClient metadataApiClient,
            IConfiguration configuration)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _aesEncryption = aesEncryption;
            _logger = logger;
            _metadataApiClient = metadataApiClient;
            _configuration = configuration;
        }

        public async Task<bool> Handle(
            NotifySupplierAwardedCommand request,
            CancellationToken cancellationToken)
        {
            var rfq = await _repository.RFQ
                .FindByCondition(x => x.Id == request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. RFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {request.RFQId}");
            }

            var externalSupplier = await _repository.ExternalSupplier
                .FindByCondition(x => x.Id == request.SupplierId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            // An external supplier row means they have no portal account yet
            // - the congratulation email must carry a registration link.
            // Otherwise it's a registered supplier - congratulate them
            // without one.
            return externalSupplier != null
                ? await NotifyExternalSupplier(externalSupplier, rfq, cancellationToken)
                : await NotifyRegisteredSupplier(request.SupplierId, rfq, cancellationToken);
        }

        private async Task<bool> NotifyExternalSupplier(
            ExternalSupplier externalSupplier,
            RFQ rfq,
            CancellationToken cancellationToken)
        {
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
                    Common.EXTERNAL_SUPPLIER_AWARD_EMAIL_KEY,
                    rfq.Id,
                    Common.EXTERNAL_SUPPLIER_ENTITY_TYPE,
                    parameters,
                    cancellationToken);

                _logger.LogInfo(
                    $"External supplier award congratulation email sent. ExternalSupplierId: {externalSupplier.Id}, RFQId: {rfq.Id}");

                return true;
            }
            catch (Exception ex)
            {
                // A mail-server hiccup must not fail an already-successful
                // RFQ award - log and continue. BaseCustomException puts the
                // actual diagnostic detail (status code/response body) in
                // Description, not Message, so both must be logged or the
                // real failure reason is lost.
                var detail = (ex as BaseCustomException)?.Description;
                _logger.LogError(
                    $"Failed to send external supplier award congratulation email. ExternalSupplierId: {externalSupplier.Id}, RFQId: {rfq.Id}. Error: {ex.Message}{(detail != null ? $" | {detail}" : string.Empty)}");

                return false;
            }
        }

        private async Task<bool> NotifyRegisteredSupplier(
            Guid supplierId,
            RFQ rfq,
            CancellationToken cancellationToken)
        {
            try
            {
                var supplier = await _supplierApiClient.GetSupplierById(
                    supplierId,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(supplier?.BusinessProfile?.Email))
                {
                    _logger.LogError(
                        $"Cannot send award congratulation email - no supplier profile/email found. SupplierId: {supplierId}, RFQId: {rfq.Id}");
                    return false;
                }

                var parameters = new Dictionary<string, string>
                {
                    { "SUPPLIER_NAME", supplier.BusinessProfile.OrganizationName ?? string.Empty },
                    { "RFQ_NUMBER", rfq.RFQNumber ?? string.Empty },
                    { "RFQ_TITLE", rfq.Title ?? string.Empty }
                };

                await _metadataApiClient.SendEmailAsync(
                    supplier.BusinessProfile.Email,
                    Common.SUPPLIER_AWARD_EMAIL_KEY,
                    rfq.Id,
                    Common.EXTERNAL_SUPPLIER_ENTITY_TYPE,
                    parameters,
                    cancellationToken);

                _logger.LogInfo(
                    $"Registered supplier award congratulation email sent. SupplierId: {supplierId}, RFQId: {rfq.Id}");

                return true;
            }
            catch (Exception ex)
            {
                var detail = (ex as BaseCustomException)?.Description;
                _logger.LogError(
                    $"Failed to send registered supplier award congratulation email. SupplierId: {supplierId}, RFQId: {rfq.Id}. Error: {ex.Message}{(detail != null ? $" | {detail}" : string.Empty)}");

                return false;
            }
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

            var origin = _configuration[Common.EXTERNAL_SUPPLIER_REGISTRATION_LINK]!;

            return $"{origin.TrimEnd('/')}/supplier/register?token={encodedToken}";
        }
    }
}
