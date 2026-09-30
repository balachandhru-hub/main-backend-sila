using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.Contracts.IServices;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services
{
    public interface IWishlistIntegrationProcessor
    {
        Task ProcessAsync(CancellationToken cancellationToken);
    }

    public class WishlistIntegrationProcessor : IWishlistIntegrationProcessor
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBuyerPurchaseDocumentGateway _buyerGateway;
        private readonly ISupplierPurchaseOrderGateway _supplierGateway;
        private readonly IUserContext _userContext;
        private readonly ILoggerManager _logger;

        public WishlistIntegrationProcessor(
            IRepositoryWrapper repository,
            IBuyerPurchaseDocumentGateway buyerGateway,
            ISupplierPurchaseOrderGateway supplierGateway,
            IUserContext userContext,
            ILoggerManager logger)
        {
            _repository = repository;
            _buyerGateway = buyerGateway;
            _supplierGateway = supplierGateway;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            List<Guid> due = await _repository.Wishlist.GetDueWishlistIdsAsync(DateTime.UtcNow, cancellationToken);
            foreach (Guid wishlistId in due)
            {
                try
                {
                    await ProcessOneAsync(wishlistId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Wishlist integration worker failed. WishlistId={wishlistId} Error={ex.Message}");
                }
            }
        }

        private async Task ProcessOneAsync(Guid wishlistId, CancellationToken cancellationToken)
        {
            Wishlist? wishlist = await _repository.Wishlist.GetTrackedByIdAsync(wishlistId, cancellationToken);
            if (wishlist == null)
            {
                return;
            }

            _userContext.SetCurrentUserId(wishlist.UpdatedBy == Guid.Empty ? wishlist.CreatedBy : wishlist.UpdatedBy);

            if (wishlist.Status == Common.WISHLIST_ERP_PROCESSING || wishlist.Status == Common.WISHLIST_ERP_FAILED)
            {
                await ProcessBuyerErpAsync(wishlist, cancellationToken);
            }

            if (wishlist.Status == Common.WISHLIST_SUPPLIER_PO_PROCESSING || wishlist.Status == Common.WISHLIST_SUPPLIER_PO_FAILED)
            {
                await ProcessSupplierErpAsync(wishlist, cancellationToken);
            }
        }

        private async Task ProcessBuyerErpAsync(Wishlist wishlist, CancellationToken cancellationToken)
        {
            PurchaseDocumentIntegration integration = await EnsureIntegrationAsync(
                wishlist,
                Common.INTEGRATION_BUYER_ERP,
                Guid.Empty,
                cancellationToken);

            if (integration.Status == Common.INTEGRATION_SUCCEEDED && !string.IsNullOrWhiteSpace(integration.ExternalDocumentNumber))
            {
                ApplyBuyerSuccess(wishlist, integration.ExternalDocumentNumber, integration.DocumentType);
                await _repository.SaveAsync();
                return;
            }

            if (IsInFlight(integration))
            {
                return;
            }

            ErpIntegrationConfiguration? configuration = await ResolveBuyerConfigurationAsync(wishlist, integration, cancellationToken);
            if (configuration == null || !configuration.IsActive)
            {
                await FailAsync(wishlist, integration, Common.WISHLIST_ERP_FAILED, Common.AUDIT_ERP_FAILED,
                    "No active buyer ERP configuration is available for this organization.", false, 400, null, cancellationToken);
                return;
            }

            PinConfiguration(integration, configuration);
            string correlationId = Guid.NewGuid().ToString("N");
            integration.Status = Common.INTEGRATION_PROCESSING;
            integration.CorrelationId = correlationId;
            integration.LastAttemptOn = DateTime.UtcNow;
            AddAudit(wishlist, Common.AUDIT_ERP_STARTED, $"CorrelationId={correlationId} ConfigurationId={configuration.Id}");
            await _repository.SaveAsync();

            List<WishlistItem> items = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(wishlist.OutletId, wishlist.BuyerId, cancellationToken);
            BuyerPurchaseDocumentRequest request = new BuyerPurchaseDocumentRequest
            {
                IdempotencyKey = integration.IdempotencyKey,
                WishlistId = wishlist.Id,
                BuyerOrganizationId = wishlist.BuyerOrganizationId,
                DocumentType = integration.DocumentType ?? configuration.DocumentType,
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
                integration.ResolvedBaseUrl!,
                integration.ResolvedPath!,
                string.IsNullOrWhiteSpace(integration.ResolvedHttpMethod) ? "POST" : integration.ResolvedHttpMethod,
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
                ApplyBuyerSuccess(wishlist, result.DocumentNumber, integration.DocumentType);
                AddAudit(wishlist, Common.AUDIT_ERP_SUCCEEDED, $"DocumentNumber={result.DocumentNumber} CorrelationId={correlationId}");
                wishlist.Status = Common.WISHLIST_SUPPLIER_PO_PROCESSING;
                await EnsureIntegrationAsync(wishlist, Common.INTEGRATION_SUPPLIER_ERP, wishlist.SupplierOrganizationId ?? Guid.Empty, cancellationToken);
                AddAudit(wishlist, Common.AUDIT_SUPPLIER_STARTED, "Queued after buyer ERP document was stored.");
                await _repository.SaveAsync();
                _logger.LogInfo(
                    $"Buyer ERP document stored. WishlistId={wishlist.Id} BuyerOrganizationId={wishlist.BuyerOrganizationId} " +
                    $"ConfigurationId={configuration.Id} CorrelationId={correlationId} DurationMs={result.DurationMs} RetryCount={integration.RetryCount}");
                return;
            }

            await FailAsync(
                wishlist,
                integration,
                Common.WISHLIST_ERP_FAILED,
                Common.AUDIT_ERP_FAILED,
                result.ErrorMessage ?? "Buyer ERP creation failed.",
                result.OutcomeUnknown,
                result.StatusCode,
                result.ResponseBody,
                cancellationToken,
                configuration.MaxRetryCount);
        }

        private async Task ProcessSupplierErpAsync(Wishlist wishlist, CancellationToken cancellationToken)
        {
            if (wishlist.SupplierOrganizationId == null || wishlist.SupplierOrganizationId == Guid.Empty)
            {
                wishlist.Status = Common.WISHLIST_SUPPLIER_PO_FAILED;
                wishlist.LastError = "Wishlist has no supplier organization, so a supplier purchase order cannot be created.";
                AddAudit(wishlist, Common.AUDIT_SUPPLIER_FAILED, wishlist.LastError);
                await _repository.SaveAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(wishlist.BuyerErpDocumentNumber))
            {
                wishlist.Status = Common.WISHLIST_SUPPLIER_PO_FAILED;
                wishlist.LastError = "Supplier purchase order was not started because the buyer ERP document number is missing.";
                AddAudit(wishlist, Common.AUDIT_SUPPLIER_FAILED, wishlist.LastError);
                await _repository.SaveAsync();
                return;
            }

            PurchaseDocumentIntegration integration = await EnsureIntegrationAsync(
                wishlist,
                Common.INTEGRATION_SUPPLIER_ERP,
                wishlist.SupplierOrganizationId.Value,
                cancellationToken);

            if (integration.Status == Common.INTEGRATION_SUCCEEDED && !string.IsNullOrWhiteSpace(integration.ExternalDocumentNumber))
            {
                wishlist.SupplierErpDocumentNumber = integration.ExternalDocumentNumber;
                wishlist.SupplierErpDocumentType = integration.DocumentType ?? Common.ERP_DOCUMENT_PO;
                wishlist.Status = Common.WISHLIST_COMPLETED;
                wishlist.LastError = null;
                await _repository.SaveAsync();
                return;
            }

            if (IsInFlight(integration))
            {
                return;
            }

            string correlationId = Guid.NewGuid().ToString("N");
            integration.Status = Common.INTEGRATION_PROCESSING;
            integration.CorrelationId = correlationId;
            integration.LastAttemptOn = DateTime.UtcNow;
            integration.DocumentType = Common.ERP_DOCUMENT_PO;
            await _repository.SaveAsync();

            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(wishlist.OutletId, wishlist.BuyerId, cancellationToken);
            List<WishlistItem> items = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            SupplierPurchaseOrderRequest request = new SupplierPurchaseOrderRequest
            {
                WishlistId = wishlist.Id,
                BuyerOrganizationId = wishlist.BuyerOrganizationId,
                SupplierOrganizationId = wishlist.SupplierOrganizationId.Value,
                IdempotencyKey = integration.IdempotencyKey,
                BuyerDocumentType = wishlist.BuyerErpDocumentType ?? Common.ERP_DOCUMENT_PO,
                BuyerDocumentNumber = wishlist.BuyerErpDocumentNumber,
                ShipTo = outlet?.ExternalShipTo,
                DeliveryInstruction = wishlist.DeliveryInstruction,
                RequiredDate = wishlist.RequiredDate,
                Currency = wishlist.Currency,
                CorrelationId = correlationId,
                Lines = items.Select(item => new SupplierPurchaseLine
                {
                    Sku = item.MaterialCode,
                    Quantity = item.Quantity,
                    UnitOfMeasure = item.UnitOfMeasure,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };

            ExternalCallResult result = await _supplierGateway.CreateSupplierPurchaseOrderAsync(request, cancellationToken);
            if (result.Succeeded && !string.IsNullOrWhiteSpace(result.DocumentNumber))
            {
                integration.Status = Common.INTEGRATION_SUCCEEDED;
                integration.ExternalDocumentNumber = result.DocumentNumber;
                integration.ResponseBody = result.ResponseBody;
                integration.ErrorMessage = null;
                integration.OutcomeUnknown = false;
                integration.NextAttemptOn = null;
                wishlist.SupplierErpDocumentNumber = result.DocumentNumber;
                wishlist.SupplierErpDocumentType = Common.ERP_DOCUMENT_PO;
                wishlist.Status = Common.WISHLIST_COMPLETED;
                wishlist.LastError = null;
                AddAudit(wishlist, Common.AUDIT_SUPPLIER_SUCCEEDED, $"DocumentNumber={result.DocumentNumber} CorrelationId={correlationId}");
                await _repository.SaveAsync();
                _logger.LogInfo(
                    $"Supplier PO stored. WishlistId={wishlist.Id} SupplierOrganizationId={wishlist.SupplierOrganizationId} " +
                    $"CorrelationId={correlationId} RetryCount={integration.RetryCount}");
                return;
            }

            ErpIntegrationConfiguration? buyerConfiguration = integration.ConfigurationId.HasValue
                ? null
                : await _repository.Wishlist.GetErpConfigurationAsync(wishlist.BuyerId, cancellationToken);
            int maxRetry = buyerConfiguration?.MaxRetryCount ?? 3;
            await FailAsync(
                wishlist,
                integration,
                Common.WISHLIST_SUPPLIER_PO_FAILED,
                Common.AUDIT_SUPPLIER_FAILED,
                result.ErrorMessage ?? "Supplier purchase order creation failed.",
                result.OutcomeUnknown,
                result.StatusCode,
                result.ResponseBody,
                cancellationToken,
                maxRetry);
        }

        private async Task<ErpIntegrationConfiguration?> ResolveBuyerConfigurationAsync(
            Wishlist wishlist,
            PurchaseDocumentIntegration integration,
            CancellationToken cancellationToken)
        {
            if (integration.ConfigurationId.HasValue)
            {
                return await _repository.Wishlist.GetErpConfigurationByIdAsync(integration.ConfigurationId.Value, cancellationToken);
            }

            return await _repository.Wishlist.GetErpConfigurationAsync(wishlist.BuyerId, cancellationToken);
        }

        private static void PinConfiguration(PurchaseDocumentIntegration integration, ErpIntegrationConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(integration.ResolvedBaseUrl))
            {
                return;
            }

            integration.ConfigurationId = configuration.Id;
            integration.ConfigurationVersion = configuration.Version;
            integration.ResolvedBaseUrl = configuration.BaseUrl;
            integration.ResolvedPath = configuration.CreateDocumentPath;
            integration.ResolvedHttpMethod = configuration.HttpMethod;
            integration.ResolvedErpType = configuration.ErpType;
            integration.DocumentType = configuration.DocumentType;
        }

        private async Task<PurchaseDocumentIntegration> EnsureIntegrationAsync(
            Wishlist wishlist,
            string integrationType,
            Guid supplierOrganizationId,
            CancellationToken cancellationToken)
        {
            PurchaseDocumentIntegration? existing = await _repository.Wishlist.GetIntegrationAsync(
                wishlist.Id,
                integrationType,
                supplierOrganizationId,
                cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            PurchaseDocumentIntegration created = new PurchaseDocumentIntegration
            {
                Id = Guid.NewGuid(),
                WishlistId = wishlist.Id,
                BuyerOrganizationId = wishlist.BuyerOrganizationId,
                SupplierOrganizationId = supplierOrganizationId,
                IntegrationType = integrationType,
                IdempotencyKey = $"{wishlist.Id}:{integrationType}:{supplierOrganizationId}",
                Status = Common.INTEGRATION_PENDING,
                IsActive = true
            };
            _repository.Wishlist.Add(created);
            await _repository.SaveAsync();
            return created;
        }

        private async Task FailAsync(
            Wishlist wishlist,
            PurchaseDocumentIntegration integration,
            string wishlistStatus,
            string auditAction,
            string error,
            bool outcomeUnknown,
            int statusCode,
            string? responseBody,
            CancellationToken cancellationToken,
            int maxRetry = 3)
        {
            integration.Status = outcomeUnknown ? Common.INTEGRATION_UNKNOWN : Common.INTEGRATION_FAILED;
            integration.RetryCount += 1;
            integration.ErrorMessage = error;
            integration.OutcomeUnknown = outcomeUnknown;
            integration.ResponseBody = responseBody;
            integration.LastAttemptOn = DateTime.UtcNow;
            integration.NextAttemptOn = !outcomeUnknown && integration.RetryCount < maxRetry
                ? DateTime.UtcNow.AddSeconds(30 * integration.RetryCount)
                : null;
            wishlist.Status = wishlistStatus;
            wishlist.LastError = error;
            AddAudit(wishlist, auditAction, $"StatusCode={statusCode} RetryCount={integration.RetryCount} OutcomeUnknown={outcomeUnknown} {error}");
            await _repository.SaveAsync();
            _logger.LogError(
                $"Integration failed. WishlistId={wishlist.Id} BuyerOrganizationId={wishlist.BuyerOrganizationId} " +
                $"SupplierOrganizationId={integration.SupplierOrganizationId} IntegrationType={integration.IntegrationType} " +
                $"ConfigurationId={integration.ConfigurationId} CorrelationId={integration.CorrelationId} " +
                $"StatusCode={statusCode} RetryCount={integration.RetryCount}");
        }

        private static bool IsInFlight(PurchaseDocumentIntegration integration)
        {
            return integration.Status == Common.INTEGRATION_PROCESSING
                   && integration.LastAttemptOn.HasValue
                   && integration.LastAttemptOn.Value > DateTime.UtcNow.AddMinutes(-10);
        }

        private static void ApplyBuyerSuccess(Wishlist wishlist, string documentNumber, string? documentType)
        {
            wishlist.BuyerErpDocumentNumber = documentNumber;
            wishlist.BuyerErpDocumentType = string.IsNullOrWhiteSpace(documentType) ? Common.ERP_DOCUMENT_PO : documentType;
            wishlist.Status = Common.WISHLIST_ERP_PO_CREATED;
            wishlist.LastError = null;
        }

        private void AddAudit(Wishlist wishlist, string action, string? detail)
        {
            _repository.Wishlist.Add(new WishlistAudit
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
