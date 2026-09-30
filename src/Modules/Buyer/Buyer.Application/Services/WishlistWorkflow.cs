using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services
{
    public class WishlistWorkflow
    {
        private static readonly HashSet<string> EditableStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Common.WISHLIST_DRAFT,
            Common.WISHLIST_REJECTED
        };

        private static readonly HashSet<string> AuthTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            Common.AUTH_NONE,
            Common.AUTH_BASIC,
            Common.AUTH_API_KEY,
            Common.AUTH_BEARER,
            Common.AUTH_OAUTH2_CLIENT_CREDENTIALS
        };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public WishlistWorkflow(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> CreateAsync(Guid organizationId, Guid userId, WishlistWriteDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            ValidateWrite(request, requireApproval: false);
            BuyerOutlet outlet = await RequireOutletAsync(request.OutletId, buyer.Id, cancellationToken);
            MasterApprovalFlow? flow = await RequireApprovalAsync(request.MasterApprovalFlowId, buyer.Id, required: false, cancellationToken);
            List<WishlistItem> items = await BuildItemsAsync(buyer.Id, Guid.Empty, request, cancellationToken);

            Wishlist wishlist = new Wishlist
            {
                Id = Guid.NewGuid(),
                BuyerOrganizationId = organizationId,
                BuyerId = buyer.Id,
                OutletId = outlet.Id,
                WishlistName = request.WishlistName.Trim(),
                Description = request.Description,
                SupplierOrganizationId = request.SupplierOrganizationId,
                SupplierName = request.SupplierName,
                Status = Common.WISHLIST_DRAFT,
                MasterApprovalFlowId = flow?.Id,
                ApprovalName = flow?.ApprovalName,
                Currency = request.Currency,
                DeliveryInstruction = request.DeliveryInstruction,
                RequiredDate = request.RequiredDate
            };
            _repository.Wishlist.Create(wishlist);
            foreach (WishlistItem item in items)
            {
                item.WishlistId = wishlist.Id;
                _repository.Wishlist.Add(item);
            }
            AddAudit(wishlist.Id, userId, Common.AUDIT_CREATED, "Wishlist created for the buyer organization.");
            await _repository.SaveAsync();
            _logger.LogInfo($"Wishlist created. WishlistId={wishlist.Id} BuyerOrganizationId={organizationId} OutletId={outlet.Id} Actor={userId}");
            return wishlist.Id;
        }

        public async Task UpdateAsync(Guid organizationId, Guid userId, Guid wishlistId, WishlistWriteDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            Wishlist wishlist = await RequireWishlistAsync(wishlistId, buyer.Id, cancellationToken);
            EnsureEditable(wishlist);
            ValidateWrite(request, requireApproval: false);
            BuyerOutlet outlet = await RequireOutletAsync(request.OutletId, buyer.Id, cancellationToken);
            MasterApprovalFlow? flow = await RequireApprovalAsync(request.MasterApprovalFlowId, buyer.Id, required: false, cancellationToken);
            List<WishlistItem> items = await BuildItemsAsync(buyer.Id, wishlist.Id, request, cancellationToken);

            wishlist.OutletId = outlet.Id;
            wishlist.WishlistName = request.WishlistName.Trim();
            wishlist.Description = request.Description;
            wishlist.SupplierOrganizationId = request.SupplierOrganizationId;
            wishlist.SupplierName = request.SupplierName;
            wishlist.MasterApprovalFlowId = flow?.Id;
            wishlist.ApprovalName = flow?.ApprovalName;
            wishlist.Currency = request.Currency;
            wishlist.DeliveryInstruction = request.DeliveryInstruction;
            wishlist.RequiredDate = request.RequiredDate;

            List<WishlistItem> existing = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            _repository.Wishlist.RemoveRange(existing);
            foreach (WishlistItem item in items)
            {
                _repository.Wishlist.Add(item);
            }
            AddAudit(wishlist.Id, userId, Common.AUDIT_MODIFIED, "Wishlist updated.");
            await _repository.SaveAsync();
        }

        public async Task<WishlistResponseDto> GetAsync(Guid organizationId, Guid wishlistId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            Wishlist wishlist = await RequireWishlistAsync(wishlistId, buyer.Id, cancellationToken);
            return await MapAsync(wishlist, cancellationToken);
        }

        public async Task<List<WishlistListItemDto>> ListAsync(Guid organizationId, int index, int limit, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            int safeIndex = index < 0 ? 0 : index;
            int safeLimit = limit <= 0 ? 20 : Math.Min(limit, 100);
            List<Wishlist> rows = await _repository.Wishlist
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.DateUpdated)
                .Skip(safeIndex)
                .Take(safeLimit)
                .ToListAsync(cancellationToken);

            List<BuyerOutlet> outlets = await _repository.Wishlist.ListOutletsAsync(buyer.Id, cancellationToken);
            Dictionary<Guid, string> outletNames = outlets.ToDictionary(x => x.Id, x => x.OutletName);
            return rows.Select(row => new WishlistListItemDto
            {
                Id = row.Id,
                WishlistName = row.WishlistName,
                OutletName = outletNames.TryGetValue(row.OutletId, out string? name) ? name : null,
                CreatedBy = row.CreatedBy,
                DateCreated = row.DateCreated,
                Status = row.Status,
                ApprovalName = row.ApprovalName,
                BuyerErpDocumentNumber = row.BuyerErpDocumentNumber,
                SupplierErpDocumentNumber = row.SupplierErpDocumentNumber,
                LastError = row.LastError
            }).ToList();
        }

        public async Task SubmitAsync(Guid organizationId, Guid userId, Guid wishlistId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            Wishlist wishlist = await RequireWishlistAsync(wishlistId, buyer.Id, cancellationToken);
            EnsureEditable(wishlist);
            if (wishlist.MasterApprovalFlowId == null)
            {
                throw new BadRequestCustomException("Approval flow is required.", "Select an approval flow before submitting the wishlist.");
            }

            List<WishlistItem> items = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            if (items.Count == 0)
            {
                throw new BadRequestCustomException("Wishlist has no items.", "Add at least one material before submitting.");
            }

            MasterApprovalFlow flow = await RequireApprovalAsync(wishlist.MasterApprovalFlowId, buyer.Id, required: true, cancellationToken)
                ?? throw new NotFoundCustomException("Approval flow not found.", "The selected approval flow does not belong to this buyer.");

            List<ApprovalFlowUserMapping> templateUsers = await _repository.ApprovalFlowUserMapping
                .FindByCondition(x => x.ApprovalFlowId == flow.Id && x.IsActive)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);
            if (templateUsers.Count == 0)
            {
                throw new BadRequestCustomException("Approval flow has no approvers.", "Add approvers to the selected approval flow.");
            }

            WishlistApprovalFlow? instance = await _repository.Wishlist.GetApprovalFlowAsync(wishlist.Id, cancellationToken);
            if (instance == null)
            {
                instance = new WishlistApprovalFlow
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlist.Id,
                    ApprovalCode = flow.ApprovalCode,
                    ApprovalName = flow.ApprovalName,
                    MasterApprovalFlowId = flow.Id,
                    Type = flow.Type,
                    TotalAmount = flow.TotalAmount,
                    Currency = flow.Currency
                };
                _repository.Wishlist.Add(instance);
            }
            else
            {
                instance.ApprovalCode = flow.ApprovalCode;
                instance.ApprovalName = flow.ApprovalName;
                instance.MasterApprovalFlowId = flow.Id;
                instance.Type = flow.Type;
                instance.TotalAmount = flow.TotalAmount;
                instance.Currency = flow.Currency;
                List<WishlistApprovalUserMapping> previous = await _repository.Wishlist.GetApprovalUsersAsync(instance.Id, cancellationToken);
                _repository.Wishlist.RemoveRange(previous);
            }

            foreach (ApprovalFlowUserMapping templateUser in templateUsers)
            {
                _repository.Wishlist.Add(new WishlistApprovalUserMapping
                {
                    Id = Guid.NewGuid(),
                    WishlistApprovalFlowId = instance.Id,
                    UserId = templateUser.UserId,
                    Order = templateUser.Order,
                    Status = Common.PENDING
                });
            }

            wishlist.Status = Common.WISHLIST_PENDING_APPROVAL;
            wishlist.ApprovalName = flow.ApprovalName;
            wishlist.SubmittedOn = DateTime.UtcNow;
            wishlist.SubmittedBy = userId;
            wishlist.LastError = null;
            AddAudit(wishlist.Id, userId, Common.AUDIT_SUBMITTED, "Wishlist submitted and frozen for approval.");
            AddAudit(wishlist.Id, userId, Common.AUDIT_APPROVAL_STARTED, $"ApprovalFlowId={flow.Id}");
            await _repository.SaveAsync();
            _logger.LogInfo($"Wishlist submitted. WishlistId={wishlist.Id} BuyerOrganizationId={organizationId} Actor={userId}");
        }

        public async Task DecideAsync(Guid organizationId, Guid userId, Guid wishlistId, WishlistDecisionDto decision, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            Wishlist wishlist = await RequireWishlistAsync(wishlistId, buyer.Id, cancellationToken);
            if (wishlist.Status != Common.WISHLIST_PENDING_APPROVAL)
            {
                throw new BadRequestCustomException("Wishlist is not pending approval.", "Only a submitted wishlist can be approved or rejected.");
            }

            if (decision.Status != Common.APPROVED && decision.Status != Common.REJECTED)
            {
                throw new BadRequestCustomException("Invalid approval status.", "Status must be APPROVE or REJECT.");
            }

            WishlistApprovalFlow? instance = await _repository.Wishlist.GetApprovalFlowAsync(wishlist.Id, cancellationToken);
            if (instance == null)
            {
                throw new NotFoundCustomException("Approval flow not found.", "This wishlist has no approval instance.");
            }

            List<WishlistApprovalUserMapping> approvers = await _repository.Wishlist.GetApprovalUsersAsync(instance.Id, cancellationToken);
            WishlistApprovalUserMapping? current = approvers.FirstOrDefault(x => x.UserId == userId && x.Status == Common.PENDING);
            if (current == null)
            {
                throw new ForBiddenCustomException("You are not the current approver.", "Only the pending approver for this wishlist can act.");
            }

            bool previousApproved = approvers
                .Where(x => x.Order < current.Order)
                .All(x => x.Status == Common.APPROVED);
            if (!previousApproved)
            {
                throw new BadRequestCustomException("Earlier approval levels are still pending.", "Approvers must act in the configured order.");
            }

            current.Status = decision.Status;
            current.Comment = decision.Comment;
            current.ActedOn = DateTime.UtcNow;

            if (decision.Status == Common.REJECTED)
            {
                wishlist.Status = Common.WISHLIST_REJECTED;
                wishlist.LastError = decision.Comment;
                AddAudit(wishlist.Id, userId, Common.AUDIT_REJECTED, decision.Comment);
                await _repository.SaveAsync();
                return;
            }

            bool finalApproval = approvers.All(x => x.Status == Common.APPROVED);
            AddAudit(wishlist.Id, userId, Common.AUDIT_APPROVED, $"Order={current.Order}");
            if (!finalApproval)
            {
                await _repository.SaveAsync();
                return;
            }

            wishlist.FinalApprovedOn = DateTime.UtcNow;
            wishlist.Status = Common.WISHLIST_ERP_PROCESSING;
            wishlist.LastError = null;
            AddAudit(wishlist.Id, userId, Common.AUDIT_ERP_STARTED, "Final approval started buyer ERP processing.");
            await _repository.SaveAsync();
            _logger.LogInfo($"Wishlist final approval. WishlistId={wishlist.Id} BuyerOrganizationId={organizationId} Actor={userId}");
        }

        public async Task CancelAsync(Guid organizationId, Guid userId, Guid wishlistId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            Wishlist wishlist = await RequireWishlistAsync(wishlistId, buyer.Id, cancellationToken);
            if (wishlist.Status is Common.WISHLIST_ERP_PROCESSING
                or Common.WISHLIST_ERP_PO_CREATED
                or Common.WISHLIST_SUPPLIER_PO_PROCESSING
                or Common.WISHLIST_COMPLETED
                or Common.WISHLIST_CANCELLED)
            {
                throw new BadRequestCustomException("Wishlist cannot be cancelled.", "ERP processing has already started or the wishlist is already closed.");
            }

            wishlist.Status = Common.WISHLIST_CANCELLED;
            AddAudit(wishlist.Id, userId, Common.AUDIT_CANCELLED, "Wishlist cancelled.");
            await _repository.SaveAsync();
        }

        public async Task RetryAsync(Guid organizationId, Guid userId, Guid wishlistId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            Wishlist wishlist = await RequireWishlistAsync(wishlistId, buyer.Id, cancellationToken);
            if (wishlist.Status == Common.WISHLIST_ERP_FAILED)
            {
                wishlist.Status = Common.WISHLIST_ERP_PROCESSING;
            }
            else if (wishlist.Status == Common.WISHLIST_SUPPLIER_PO_FAILED)
            {
                wishlist.Status = Common.WISHLIST_SUPPLIER_PO_PROCESSING;
            }
            else
            {
                throw new BadRequestCustomException("Nothing to retry.", "Retry is available only after an ERP or supplier integration failure.");
            }

            List<PurchaseDocumentIntegration> integrations = await _repository.Wishlist.GetIntegrationsAsync(wishlist.Id, cancellationToken);
            foreach (PurchaseDocumentIntegration integration in integrations)
            {
                if (integration.Status is Common.INTEGRATION_FAILED or Common.INTEGRATION_UNKNOWN)
                {
                    integration.Status = Common.INTEGRATION_PENDING;
                    integration.NextAttemptOn = DateTime.UtcNow;
                    integration.OutcomeUnknown = false;
                }
            }

            wishlist.LastError = null;
            AddAudit(wishlist.Id, userId, Common.AUDIT_RETRY, "Integration retry requested. Existing document numbers are reused.");
            await _repository.SaveAsync();
        }

        public async Task<Guid> CreateOutletAsync(Guid organizationId, OutletWriteDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            if (string.IsNullOrWhiteSpace(request.OutletName))
            {
                throw new BadRequestCustomException("Outlet name is required.", "Enter an outlet name.");
            }

            BuyerOutlet outlet = new BuyerOutlet
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                OutletName = request.OutletName.Trim(),
                OutletCode = request.OutletCode,
                Description = request.Description,
                ExternalShipTo = request.ExternalShipTo,
                AddressLine1 = request.AddressLine1,
                City = request.City,
                Country = request.Country
            };
            _repository.Wishlist.Add(outlet);
            await _repository.SaveAsync();
            return outlet.Id;
        }

        public async Task<List<OutletResponseDto>> ListOutletsAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            List<BuyerOutlet> outlets = await _repository.Wishlist.ListOutletsAsync(buyer.Id, cancellationToken);
            return outlets.Select(MapOutlet).ToList();
        }

        public async Task<ErpIntegrationResponseDto?> GetErpConfigurationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            ErpIntegrationConfiguration? configuration = await _repository.Wishlist.GetErpConfigurationAsync(buyer.Id, cancellationToken);
            return configuration == null ? null : MapConfiguration(configuration);
        }

        public async Task<Guid> SaveErpConfigurationAsync(Guid organizationId, ErpIntegrationWriteDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await GetBuyerAsync(organizationId);
            ValidateConfiguration(request);
            ErpIntegrationConfiguration? existing = request.SupplierOrganizationId.HasValue
                ? await _repository.Wishlist.GetSupplierErpConfigurationAsync(buyer.Id, request.SupplierOrganizationId.Value, cancellationToken)
                : await _repository.Wishlist.GetErpConfigurationAsync(buyer.Id, cancellationToken);
            if (existing == null)
            {
                ErpIntegrationConfiguration created = new ErpIntegrationConfiguration
                {
                    Id = Guid.NewGuid(),
                    BuyerOrganizationId = organizationId,
                    BuyerId = buyer.Id,
                    Version = 1
                };
                ApplyConfiguration(created, request, keepSecrets: false);
                _repository.Wishlist.Add(created);
                await _repository.SaveAsync();
                return created.Id;
            }

            ApplyConfiguration(existing, request, keepSecrets: true);
            existing.Version += 1;
            existing.IsActive = request.IsActive;
            await _repository.SaveAsync();
            return existing.Id;
        }

        private async Task<WishlistResponseDto> MapAsync(Wishlist wishlist, CancellationToken cancellationToken)
        {
            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(wishlist.OutletId, wishlist.BuyerId, cancellationToken);
            List<WishlistItem> items = await _repository.Wishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            WishlistApprovalFlow? flow = await _repository.Wishlist.GetApprovalFlowAsync(wishlist.Id, cancellationToken);
            List<WishlistApprovalUserMapping> steps = flow == null
                ? new List<WishlistApprovalUserMapping>()
                : await _repository.Wishlist.GetApprovalUsersAsync(flow.Id, cancellationToken);
            List<WishlistAudit> audit = await _repository.Wishlist.GetAuditAsync(wishlist.Id, cancellationToken);
            List<PurchaseDocumentIntegration> integrations = await _repository.Wishlist.GetIntegrationsAsync(wishlist.Id, cancellationToken);

            return new WishlistResponseDto
            {
                Id = wishlist.Id,
                BuyerOrganizationId = wishlist.BuyerOrganizationId,
                BuyerId = wishlist.BuyerId,
                OutletId = wishlist.OutletId,
                OutletName = outlet?.OutletName,
                WishlistName = wishlist.WishlistName,
                Description = wishlist.Description,
                SupplierOrganizationId = wishlist.SupplierOrganizationId,
                SupplierName = wishlist.SupplierName,
                Status = wishlist.Status,
                MasterApprovalFlowId = wishlist.MasterApprovalFlowId,
                ApprovalName = wishlist.ApprovalName,
                CreatedBy = wishlist.CreatedBy,
                DateCreated = wishlist.DateCreated,
                UpdatedBy = wishlist.UpdatedBy,
                DateUpdated = wishlist.DateUpdated,
                SubmittedOn = wishlist.SubmittedOn,
                FinalApprovedOn = wishlist.FinalApprovedOn,
                BuyerErpDocumentType = wishlist.BuyerErpDocumentType,
                BuyerErpDocumentNumber = wishlist.BuyerErpDocumentNumber,
                SupplierErpDocumentNumber = wishlist.SupplierErpDocumentNumber,
                Currency = wishlist.Currency,
                DeliveryInstruction = wishlist.DeliveryInstruction,
                RequiredDate = wishlist.RequiredDate,
                LastError = wishlist.LastError,
                IsFrozen = !EditableStatuses.Contains(wishlist.Status),
                Items = items.Select(item => new WishlistItemResponseDto
                {
                    Id = item.Id,
                    MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode,
                    MaterialName = item.MaterialName,
                    UnitOfMeasure = item.UnitOfMeasure,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Currency = item.Currency,
                    RequiredDate = item.RequiredDate
                }).ToList(),
                ApprovalSteps = steps.Select(step => new WishlistApprovalStepDto
                {
                    UserId = step.UserId,
                    Order = step.Order,
                    Status = step.Status,
                    Comment = step.Comment,
                    ActedOn = step.ActedOn
                }).ToList(),
                Audit = audit.Select(entry => new WishlistAuditDto
                {
                    Action = entry.Action,
                    Detail = entry.Detail,
                    ActorUserId = entry.ActorUserId,
                    DateCreated = entry.DateCreated
                }).ToList(),
                Integrations = integrations.Select(entry => new WishlistIntegrationDto
                {
                    IntegrationType = entry.IntegrationType,
                    Status = entry.Status,
                    DocumentType = entry.DocumentType,
                    ExternalDocumentNumber = entry.ExternalDocumentNumber,
                    ErrorMessage = entry.ErrorMessage,
                    RetryCount = entry.RetryCount,
                    LastAttemptOn = entry.LastAttemptOn,
                    NextAttemptOn = entry.NextAttemptOn,
                    CorrelationId = entry.CorrelationId,
                    OutcomeUnknown = entry.OutcomeUnknown,
                    ConfigurationId = entry.ConfigurationId
                }).ToList()
            };
        }

        private async Task<List<WishlistItem>> BuildItemsAsync(Guid buyerId, Guid wishlistId, WishlistWriteDto request, CancellationToken cancellationToken)
        {
            List<WishlistItem> items = new List<WishlistItem>();
            foreach (WishlistItemWriteDto line in request.Items ?? new List<WishlistItemWriteDto>())
            {
                if (line.Quantity <= 0)
                {
                    throw new BadRequestCustomException("Quantity must be greater than zero.", "Enter a quantity for every material.");
                }

                ItemBuyerMaster? material = await _repository.ItemBuyerMaster.FindFirstByConditionAsync(
                    x => x.Id == line.MaterialId && x.BuyerId == buyerId && x.IsActive);
                if (material == null)
                {
                    throw new NotFoundCustomException("Material not found.", "Select a material from the buyer catalog.");
                }

                items.Add(new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    WishlistId = wishlistId,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode,
                    MaterialName = material.Description,
                    UnitOfMeasure = material.OrderUnitOfMeasure ?? material.BaseUnitOfMeasure,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Currency = line.Currency ?? request.Currency,
                    RequiredDate = line.RequiredDate ?? request.RequiredDate
                });
            }

            return items;
        }

        private async Task<BuyerBusinessProfile> GetBuyerAsync(Guid organizationId)
        {
            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == organizationId && x.IsActive);
            if (buyer == null)
            {
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            return buyer;
        }

        private async Task<Wishlist> RequireWishlistAsync(Guid wishlistId, Guid buyerId, CancellationToken cancellationToken)
        {
            Wishlist? wishlist = await _repository.Wishlist.GetTrackedAsync(wishlistId, buyerId, cancellationToken);
            if (wishlist == null)
            {
                throw new NotFoundCustomException("Wishlist not found.", "No wishlist exists for this buyer organization.");
            }

            return wishlist;
        }

        private async Task<BuyerOutlet> RequireOutletAsync(Guid outletId, Guid buyerId, CancellationToken cancellationToken)
        {
            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(outletId, buyerId, cancellationToken);
            if (outlet == null)
            {
                throw new NotFoundCustomException("Outlet not found.", "Select an outlet that belongs to this buyer organization.");
            }

            return outlet;
        }

        private async Task<MasterApprovalFlow?> RequireApprovalAsync(Guid? approvalFlowId, Guid buyerId, bool required, CancellationToken cancellationToken)
        {
            if (approvalFlowId == null || approvalFlowId == Guid.Empty)
            {
                if (required)
                {
                    throw new BadRequestCustomException("Approval flow is required.", "Select an approval flow.");
                }

                return null;
            }

            MasterApprovalFlow? flow = await _repository.MasterApprovalFlow.FindFirstByConditionAsync(
                x => x.Id == approvalFlowId && x.BuyerId == buyerId && x.IsActive);
            if (flow == null)
            {
                throw new NotFoundCustomException("Approval flow not found.", "The approval flow does not belong to this buyer organization.");
            }

            if (!string.Equals(flow.Type, Common.WISHLIST_APPROVAL_TYPE, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Approval flow type is not Wishlist.", "Select an approval configuration of type WISHLIST.");
            }

            return flow;
        }

        private static void EnsureEditable(Wishlist wishlist)
        {
            if (!EditableStatuses.Contains(wishlist.Status))
            {
                throw new BadRequestCustomException(
                    "Wishlist is frozen.",
                    "Submitted wishlists cannot be edited until they are rejected.");
            }
        }

        private static void ValidateWrite(WishlistWriteDto request, bool requireApproval)
        {
            if (request.OutletId == Guid.Empty)
            {
                throw new BadRequestCustomException("Outlet is required.", "Select an outlet.");
            }

            if (string.IsNullOrWhiteSpace(request.WishlistName))
            {
                throw new BadRequestCustomException("Wishlist name is required.", "Enter a wishlist name.");
            }

            if (requireApproval && (request.MasterApprovalFlowId == null || request.MasterApprovalFlowId == Guid.Empty))
            {
                throw new BadRequestCustomException("Approval flow is required.", "Select an approval flow.");
            }
        }

        private static void ValidateConfiguration(ErpIntegrationWriteDto request)
        {
            if (string.IsNullOrWhiteSpace(request.ErpType)
                || string.IsNullOrWhiteSpace(request.BaseUrl)
                || string.IsNullOrWhiteSpace(request.CreateDocumentPath)
                || string.IsNullOrWhiteSpace(request.AuthType)
                || string.IsNullOrWhiteSpace(request.DocumentType))
            {
                throw new BadRequestCustomException(
                    "ERP configuration is incomplete.",
                    "ERP type, document type, base URL, endpoint path, and authentication type are required.");
            }

            if (!request.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !request.BaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Base URL is invalid.", "Enter an absolute http or https base URL.");
            }

            if (!AuthTypes.Contains(request.AuthType))
            {
                throw new BadRequestCustomException("Authentication type is not supported.", "Use NONE, BASIC, API_KEY, BEARER, or OAUTH2_CLIENT_CREDENTIALS.");
            }

            if (request.DocumentType != Common.ERP_DOCUMENT_PO && request.DocumentType != Common.ERP_DOCUMENT_PR)
            {
                throw new BadRequestCustomException("Document type is invalid.", "Use PO or PR.");
            }
        }

        private static void ApplyConfiguration(ErpIntegrationConfiguration target, ErpIntegrationWriteDto request, bool keepSecrets)
        {
            target.ErpType = request.ErpType.Trim();
            target.SupplierOrganizationId = request.SupplierOrganizationId;
            target.PayloadFormat = string.IsNullOrWhiteSpace(request.PayloadFormat) ? Common.PAYLOAD_JSON : request.PayloadFormat.Trim();
            target.DocumentType = request.DocumentType.Trim();
            target.BaseUrl = request.BaseUrl.Trim();
            target.CreateDocumentPath = request.CreateDocumentPath.Trim();
            target.HttpMethod = string.IsNullOrWhiteSpace(request.HttpMethod) ? "POST" : request.HttpMethod.Trim();
            target.AuthType = request.AuthType.Trim();
            target.TokenUrl = request.TokenUrl;
            target.Username = request.Username;
            target.ClientId = request.ClientId;
            target.Scope = request.Scope;
            target.ApiKeyHeader = request.ApiKeyHeader;
            target.HeadersJson = request.HeadersJson;
            target.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 60 : request.TimeoutSeconds;
            target.MaxRetryCount = request.MaxRetryCount < 0 ? 0 : request.MaxRetryCount;
            target.IsActive = request.IsActive;
            target.Password = Secret(request.Password, target.Password, keepSecrets);
            target.ClientSecret = Secret(request.ClientSecret, target.ClientSecret, keepSecrets);
            target.ApiKey = Secret(request.ApiKey, target.ApiKey, keepSecrets);
            target.AccessToken = Secret(request.AccessToken, target.AccessToken, keepSecrets);
        }

        private static string? Secret(string? incoming, string? current, bool keepSecrets)
        {
            if (!string.IsNullOrWhiteSpace(incoming))
            {
                return incoming;
            }

            return keepSecrets ? current : incoming;
        }

        private static ErpIntegrationResponseDto MapConfiguration(ErpIntegrationConfiguration configuration)
        {
            return new ErpIntegrationResponseDto
            {
                Id = configuration.Id,
                ErpType = configuration.ErpType,
                SupplierOrganizationId = configuration.SupplierOrganizationId,
                PayloadFormat = configuration.PayloadFormat,
                DocumentType = configuration.DocumentType,
                BaseUrl = configuration.BaseUrl,
                CreateDocumentPath = configuration.CreateDocumentPath,
                HttpMethod = configuration.HttpMethod,
                AuthType = configuration.AuthType,
                TokenUrl = configuration.TokenUrl,
                Username = configuration.Username,
                HasPassword = !string.IsNullOrWhiteSpace(configuration.Password),
                ClientId = configuration.ClientId,
                HasClientSecret = !string.IsNullOrWhiteSpace(configuration.ClientSecret),
                Scope = configuration.Scope,
                ApiKeyHeader = configuration.ApiKeyHeader,
                HasApiKey = !string.IsNullOrWhiteSpace(configuration.ApiKey),
                HasAccessToken = !string.IsNullOrWhiteSpace(configuration.AccessToken),
                HeadersJson = configuration.HeadersJson,
                TimeoutSeconds = configuration.TimeoutSeconds,
                MaxRetryCount = configuration.MaxRetryCount,
                Version = configuration.Version,
                IsActive = configuration.IsActive
            };
        }

        private static OutletResponseDto MapOutlet(BuyerOutlet outlet)
        {
            return new OutletResponseDto
            {
                Id = outlet.Id,
                OutletName = outlet.OutletName,
                OutletCode = outlet.OutletCode,
                Description = outlet.Description,
                ExternalShipTo = outlet.ExternalShipTo,
                AddressLine1 = outlet.AddressLine1,
                City = outlet.City,
                Country = outlet.Country
            };
        }

        private void AddAudit(Guid wishlistId, Guid userId, string action, string? detail)
        {
            _repository.Wishlist.Add(new WishlistAudit
            {
                Id = Guid.NewGuid(),
                WishlistId = wishlistId,
                Action = action,
                Detail = detail,
                ActorUserId = userId
            });
        }
    }
}
