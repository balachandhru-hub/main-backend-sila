using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.Contracts.IServices;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services
{
    /// <summary>
    /// Sends the purchase order to every API configured for PO_CREATE.
    /// Called from the final approval request, so the send is not waiting on a poll.
    /// </summary>
    public class WishlistIntegrationProcessor
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBuyerPurchaseDocumentGateway _buyerGateway;
        private readonly IUserContext _userContext;
        private readonly ILoggerManager _logger;

        public WishlistIntegrationProcessor(
            IRepositoryWrapper repository,
            IBuyerPurchaseDocumentGateway buyerGateway,
            IUserContext userContext,
            ILoggerManager logger)
        {
            _repository = repository;
            _buyerGateway = buyerGateway;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task SendPurchaseOrdersAsync(Guid wishlistId, Guid actorUserId, CancellationToken cancellationToken)
        {
            _userContext.SetCurrentUserId(actorUserId);
            Wishlist? wishlist = await _repository.Wishlist.GetTrackedByIdAsync(wishlistId, cancellationToken);
            if (wishlist == null)
            {
                _logger.LogError($"Wishlist not found while sending purchase orders. WishlistId: {wishlistId}");
                return;
            }

            List<ErpIntegrationConfiguration> configurations = await _repository.ErpIntegration.ListForOperationAsync(
                wishlist.BuyerId,
                Common.ERP_OPERATION_PO_CREATE,
                wishlist.SupplierOrganizationId,
                cancellationToken);
            if (configurations.Count == 0)
            {
                wishlist.Status = Common.WISHLIST_ERP_FAILED;
                wishlist.LastError = "No purchase order API is configured. Add an API with type PO_CREATE.";
                AddAudit(wishlist, Common.AUDIT_ERP_FAILED, wishlist.LastError);
                await _repository.SaveAsync();
                _logger.LogError($"No PO_CREATE API is configured. WishlistId: {wishlist.Id}, BuyerId: {wishlist.BuyerId}");
                return;
            }

            List<WishlistItem> items = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(wishlist.OutletId, wishlist.BuyerId, cancellationToken);
            List<PurchaseDocumentIntegration> existing = await _repository.Wishlist.GetIntegrationsAsync(wishlist.Id, cancellationToken);
            List<string> documents = new List<string>();
            List<string> failures = new List<string>();

            foreach (ErpIntegrationConfiguration configuration in configurations)
            {
                string? documentNumber = await SendOneAsync(wishlist, configuration, items, outlet, existing, cancellationToken);
                if (string.IsNullOrWhiteSpace(documentNumber))
                {
                    failures.Add($"{configuration.ErpType}: {wishlist.LastError}");
                    continue;
                }

                documents.Add($"{configuration.ErpType}: {documentNumber}");
            }

            if (documents.Count > 0)
            {
                wishlist.BuyerErpDocumentNumber = string.Join("; ", documents);
                wishlist.BuyerErpDocumentType = Common.ERP_DOCUMENT_PO;
            }

            if (failures.Count == 0)
            {
                wishlist.Status = Common.WISHLIST_COMPLETED;
                wishlist.LastError = null;
                await _repository.SaveAsync();
                _logger.LogInfo($"Purchase orders sent. WishlistId: {wishlist.Id}, Documents: {wishlist.BuyerErpDocumentNumber}");
                return;
            }

            wishlist.Status = Common.WISHLIST_ERP_FAILED;
            wishlist.LastError = string.Join(" ", failures);
            await _repository.SaveAsync();
            _logger.LogError($"Purchase order send failed. WishlistId: {wishlist.Id}, Error: {wishlist.LastError}");
        }

        private async Task<string?> SendOneAsync(
            Wishlist wishlist,
            ErpIntegrationConfiguration configuration,
            List<WishlistItem> items,
            BuyerOutlet? outlet,
            List<PurchaseDocumentIntegration> existing,
            CancellationToken cancellationToken)
        {
            PurchaseDocumentIntegration integration = await EnsureIntegrationAsync(wishlist, configuration, existing, cancellationToken);
            if (integration.Status == Common.INTEGRATION_SUCCEEDED && !string.IsNullOrWhiteSpace(integration.ExternalDocumentNumber))
            {
                return integration.ExternalDocumentNumber;
            }

            string correlationId = Guid.NewGuid().ToString("N");
            integration.Status = Common.INTEGRATION_PROCESSING;
            integration.CorrelationId = correlationId;
            integration.LastAttemptOn = DateTime.UtcNow;
            integration.DocumentType = string.IsNullOrWhiteSpace(configuration.DocumentType)
                ? Common.ERP_DOCUMENT_PO
                : configuration.DocumentType;
            AddAudit(wishlist, Common.AUDIT_ERP_STARTED, $"System={configuration.ErpType} ConfigurationId={configuration.Id} CorrelationId={correlationId}");
            await _repository.SaveAsync();

            BuyerPurchaseDocumentRequest request = new BuyerPurchaseDocumentRequest
            {
                IdempotencyKey = integration.IdempotencyKey,
                WishlistId = wishlist.Id,
                BuyerOrganizationId = wishlist.BuyerOrganizationId,
                DocumentType = integration.DocumentType ?? Common.ERP_DOCUMENT_PO,
                BuyerDocumentNumber = wishlist.BuyerErpDocumentNumber,
                ShipTo = outlet?.ExternalShipTo,
                OutletCode = outlet?.OutletCode,
                OutletName = outlet?.OutletName,
                Currency = wishlist.Currency,
                DeliveryInstruction = wishlist.DeliveryInstruction,
                RequiredDate = wishlist.RequiredDate,
                CorrelationId = correlationId,
                Lines = items.Select(item => new BuyerPurchaseLine
                {
                    MaterialCode = item.MaterialCode,
                    MaterialName = item.MaterialName,
                    Quantity = item.Quantity,
                    UnitOfMeasure = item.UnitOfMeasure,
                    UnitPrice = item.UnitPrice,
                    Currency = item.Currency ?? wishlist.Currency
                }).ToList()
            };

            ExternalCallResult result = await _buyerGateway.CreateBuyerPurchaseDocumentAsync(
                configuration,
                configuration.BaseUrl,
                configuration.CreateDocumentPath,
                string.IsNullOrWhiteSpace(configuration.HttpMethod) ? "POST" : configuration.HttpMethod,
                request,
                cancellationToken);

            if (result.Succeeded && !string.IsNullOrWhiteSpace(result.DocumentNumber))
            {
                integration.Status = Common.INTEGRATION_SUCCEEDED;
                integration.ExternalDocumentNumber = result.DocumentNumber;
                integration.ResponseBody = result.ResponseBody;
                integration.ErrorMessage = null;
                integration.OutcomeUnknown = false;
                integration.NextAttemptOn = null;
                AddAudit(wishlist, Common.AUDIT_ERP_SUCCEEDED, $"System={configuration.ErpType} DocumentNumber={result.DocumentNumber} CorrelationId={correlationId}");
                await _repository.SaveAsync();
                _logger.LogInfo(
                    $"Purchase order stored. WishlistId={wishlist.Id} System={configuration.ErpType} ConfigurationId={configuration.Id} " +
                    $"CorrelationId={correlationId} DurationMs={result.DurationMs}");
                return result.DocumentNumber;
            }

            string error = result.ErrorMessage ?? "Purchase order creation failed.";
            integration.Status = result.OutcomeUnknown ? Common.INTEGRATION_UNKNOWN : Common.INTEGRATION_FAILED;
            integration.RetryCount += 1;
            integration.ErrorMessage = error;
            integration.OutcomeUnknown = result.OutcomeUnknown;
            integration.ResponseBody = result.ResponseBody;
            integration.LastAttemptOn = DateTime.UtcNow;
            integration.NextAttemptOn = null;
            wishlist.LastError = error;
            AddAudit(wishlist, Common.AUDIT_ERP_FAILED, $"System={configuration.ErpType} StatusCode={result.StatusCode} {error}");
            await _repository.SaveAsync();
            _logger.LogError(
                $"Purchase order call failed. WishlistId={wishlist.Id} System={configuration.ErpType} ConfigurationId={configuration.Id} " +
                $"CorrelationId={correlationId} StatusCode={result.StatusCode}");
            return null;
        }

        private async Task<PurchaseDocumentIntegration> EnsureIntegrationAsync(
            Wishlist wishlist,
            ErpIntegrationConfiguration configuration,
            List<PurchaseDocumentIntegration> existing,
            CancellationToken cancellationToken)
        {
            PurchaseDocumentIntegration? integration = existing.FirstOrDefault(x => x.ConfigurationId == configuration.Id);
            if (integration != null)
            {
                return integration;
            }

            integration = new PurchaseDocumentIntegration
            {
                Id = Guid.NewGuid(),
                WishlistId = wishlist.Id,
                BuyerOrganizationId = wishlist.BuyerOrganizationId,
                SupplierOrganizationId = configuration.SupplierOrganizationId ?? Guid.Empty,
                IntegrationType = Common.ERP_OPERATION_PO_CREATE,
                IdempotencyKey = $"{wishlist.Id}:{configuration.Id}",
                ConfigurationId = configuration.Id,
                ConfigurationVersion = configuration.Version,
                ResolvedBaseUrl = configuration.BaseUrl,
                ResolvedPath = configuration.CreateDocumentPath,
                ResolvedHttpMethod = configuration.HttpMethod,
                ResolvedErpType = configuration.ErpType,
                DocumentType = configuration.DocumentType,
                Status = Common.INTEGRATION_PENDING,
                IsActive = true
            };
            _repository.PurchaseDocumentIntegration.Create(integration);
            existing.Add(integration);
            await _repository.SaveAsync();
            return integration;
        }

        private void AddAudit(Wishlist wishlist, string action, string? detail)
        {
            _repository.WishlistAudit.Create(new WishlistAudit
            {
                Id = Guid.NewGuid(),
                WishlistId = wishlist.Id,
                Action = action,
                Detail = detail,
                ActorUserId = _userContext.GetCurrentUserId()
            });
        }
    }
}
