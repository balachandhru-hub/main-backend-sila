using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;
using Supplier.Application.Contracts;

namespace Supplier.Application.Features.Queries.GetSupplierAllRFQ
{
    public class GetSupplierRFQByIdQueryHandler : IRequestHandler<GetSupplierRFQByIdQuery, GetRFQByIdDto>
    {
        private readonly IRepositoryWrapper _repositorywrapper;
        private readonly IMetadataApiClient _metadataClient;
        private readonly ILoggerManager _logger;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly IIdentityApiClient _identityApiClient;

        public GetSupplierRFQByIdQueryHandler(IRepositoryWrapper repository, IMetadataApiClient metadataApiClient, ILoggerManager logger, IBuyerApiClient buyerApiClient, IIdentityApiClient identityApiClient)
        {
            _repositorywrapper = repository;
            _metadataClient = metadataApiClient;
            _logger = logger;
            _buyerApiClient = buyerApiClient;
            _identityApiClient = identityApiClient;
        }

        public async Task<GetRFQByIdDto> Handle(
            GetSupplierRFQByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching RFQ details for RFQId: {request.RFQId}");

            var supplier = _repositorywrapper.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                throw new PreConditionFailedCustomException(
                    "Supplier not found.",
                    $"Supplier not found for the given organization : {request.OrganizationId}");
            }

            // One BuyerRFQ fans out to one SupplierRFQ per invited supplier,
            // so the lookup MUST be scoped to the calling supplier. Filtering
            // by BuyerRFQId alone returns an arbitrary supplier's RFQ (and
            // therefore that supplier's SupplierRFQItem / quotation IDs).
            var rfq = await _repositorywrapper.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.SupplierId == supplier.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError(
                    $"RFQ not found for BuyerRFQId: {request.RFQId} and SupplierId: {supplier.Id}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with BuyerRFQId: {request.RFQId} for this supplier.");
            }

            if (!request.RoleId.Equals(Common.SUPPLIER_ADMIN_ROLE_ID))
            {
                var isInvited = await _repositorywrapper.RFQOrganizationUserMapping
                    .FindByCondition(x =>
                        x.BuyerRFQId == rfq.BuyerRFQId &&
                        x.UserId == request.UserId &&
                        x.IsActive)
                    .AnyAsync(cancellationToken);

                if (!isInvited)
                {
                    throw new ForBiddenCustomException(
                        "Access denied.",
                        "You have not been invited to this RFQ.");
                }
            }

            var buyerId = rfq.BuyerId;
            var supplierRFQId = rfq.Id;
            _logger.LogInfo($"Fetching verified suppliers for BuyerId: {buyerId}");

            _logger.LogInfo($"Fetching Buyer Name for BuyerId: {buyerId}");
            var buyerName = await _buyerApiClient.GetBuyerNameById(
                buyerId,
                cancellationToken);

            var attachmentResponse = await _buyerApiClient.GetRFQAttachments(
                request.RFQId,
                cancellationToken);
            var questions = await _buyerApiClient.GetRFQQuestions(
                request.RFQId,
                cancellationToken);

            _logger.LogInfo($"Fetching RFQ items for SupplierRFQId: {supplierRFQId}");
            var rfqItems = await _repositorywrapper.SupplierRFQItem
                .FindByCondition(x => x.SupplierRFQId == supplierRFQId)
                   .OrderBy(x => x.LineNumber)
                .ToListAsync(cancellationToken);

            var items = new List<GetRFQItemDto>();

            foreach (var item in rfqItems)
            {
                _logger.LogInfo($"Fetching attachments for RFQItemId: {item.BuyerRFQItemId}");
                var itemAttachment = attachmentResponse.ItemAttachments
                    .FirstOrDefault(x => x.RFQItemId == item.BuyerRFQItemId);

                string? costCenterName = null;

                if (!string.IsNullOrWhiteSpace(item.CostCenter) &&
                    Guid.TryParse(item.CostCenter, out var costCenterId))
                {
                    _logger.LogInfo(
                        $"Fetching Cost Center for CostCenterId: {costCenterId}");

                    var costCenter = await _buyerApiClient.GetCostCenterById(
                        costCenterId,
                        cancellationToken);

                    costCenterName = costCenter?.CostCenter;
                }
                items.Add(new GetRFQItemDto
                {
                    Id = item.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UOM = item.UOM,
                    MaterialCode = item.MaterialCode,
                    MaterialGroup = item.MaterialGroup,
                    CostCenter = item.CostCenter,
                    CostCenterName = costCenterName,
                    Attachments = itemAttachment?.Attachments ?? new List<AssetDto>(),
                    SupplierRFQItemId = item.Id,
                    SupplierRFQId = item.SupplierRFQId,
                    BuyerRFQItemId = item.BuyerRFQItemId,
                    LineNumber = item.LineNumber,
                    IsAwarded = item.IsAwarded,
                    AwardedSupplierId = item.AwardedSupplierId


                });
            }

            _logger.LogInfo($"Fetching technical specification and terms documents for RFQId: {request.RFQId}");
            var technicalDocuments = attachmentResponse.TechnicalSpecificationDocuments;
            var termsDocuments = attachmentResponse.TermsConditionDocuments;

            _logger.LogInfo($"Fetching E-Sign document for SupplierRFQId: {supplierRFQId}");
            var esignMappings = await _repositorywrapper.RFQAttachmentMapping
                .FindByCondition(x =>
                    x.SupplierRFQId == supplierRFQId &&
                    x.Type == Common.ESIGN &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            List<AssetDto> esignDocuments = new();

            if (esignMappings.Any())
            {
                var esignAssetIds = esignMappings
                    .Select(x => x.AssetId)
                    .Distinct()
                    .ToList();

                var esignAssets = await _repositorywrapper.Asset
                    .FindByCondition(x => esignAssetIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync(cancellationToken);

                var esignMetadataList = await _metadataClient.GetReferenceList(
                    new List<string> { Common.ASSET_TYPE, Common.FILE_TYPE });

                esignDocuments = esignAssets.Select(asset => new AssetDto
                {
                    Id = asset.Id,
                    AssetType = esignMetadataList.FirstOrDefault(x =>
                        x.Type == Common.ASSET_TYPE &&
                        x.Id == asset.AssetType)?.Key ?? string.Empty,
                    AssetName = asset.AssetName,
                    FileType = esignMetadataList.FirstOrDefault(x =>
                        x.Type == Common.FILE_TYPE &&
                        x.Id == asset.FileType)?.Key ?? string.Empty,
                    FileName = asset.FileName
                }).ToList();
            }

            _logger.LogInfo($"Fetching Terms and Condition document for SupplierRFQId: {supplierRFQId}");
            var supplierTermsConditionMappings = await _repositorywrapper.RFQAttachmentMapping
                .FindByCondition(x =>
                    x.SupplierRFQId == supplierRFQId &&
                    x.Type == Common.TERMS_CONDITION &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            List<AssetDto> supplierTermsConditionDocuments = new();

            if (supplierTermsConditionMappings.Any())
            {
                var termsConditionAssetIds = supplierTermsConditionMappings
                    .Select(x => x.AssetId)
                    .Distinct()
                    .ToList();

                var termsConditionAssets = await _repositorywrapper.Asset
                    .FindByCondition(x => termsConditionAssetIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync(cancellationToken);

                var termsConditionMetadataList = await _metadataClient.GetReferenceList(
                    new List<string> { Common.ASSET_TYPE, Common.FILE_TYPE });

                supplierTermsConditionDocuments = termsConditionAssets.Select(asset => new AssetDto
                {
                    Id = asset.Id,
                    AssetType = termsConditionMetadataList.FirstOrDefault(x =>
                        x.Type == Common.ASSET_TYPE &&
                        x.Id == asset.AssetType)?.Key ?? string.Empty,
                    AssetName = asset.AssetName,
                    FileType = termsConditionMetadataList.FirstOrDefault(x =>
                        x.Type == Common.FILE_TYPE &&
                        x.Id == asset.FileType)?.Key ?? string.Empty,
                    FileName = asset.FileName
                }).ToList();
            }

            _logger.LogInfo($"Fetching Supplier Terms and Condition status for BuyerRFQId: {request.RFQId}");
            string supplierTermsAndConditionAccepted;

            try
            {
                var supplierTermsConditionStatus = await _buyerApiClient.GetSupplierTermsConditionStatus(
                    request.RFQId,
                    supplier.Id,
                    cancellationToken);

                supplierTermsAndConditionAccepted =
                    supplierTermsConditionStatus?.Status ?? Common.PENDING;
            }
            catch
            {
                _logger.LogInfo(
                    $"No Supplier Terms and Condition status found for BuyerRFQId: {request.RFQId}");
                supplierTermsAndConditionAccepted = Common.PENDING;
            }
            // Supplier Quotation Header
            var quotation = await _repositorywrapper.SupplierQuotation
                .FindByCondition(x => x.SupplierRFQId == supplierRFQId)
                .FirstOrDefaultAsync(cancellationToken);

            var lowestQuotation = await _repositorywrapper.SupplierQuotation
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.IsActive)
                .OrderBy(x => x.TotalPrice)
                .FirstOrDefaultAsync(cancellationToken);

            // Supplier Quotation Items
            var quotationItems = new List<SupplierQuotationItemDto>();

            if (quotation != null)
            {
                quotationItems = await _repositorywrapper.SupplierQuotationItem
                    .FindByCondition(x => x.SupplierQuotationId == quotation.Id)
                    .Select(x => new SupplierQuotationItemDto
                    {
                        ItemQuotationId = x.Id,
                        QuotedPrice = x.QuotedPrice
                    })
                    .ToListAsync(cancellationToken);
            }

            // Once the RFQ is awarded, read this supplier's contract status from the
            // Contract table (Buyer service). It stays null until a contract exists.
            string? contractStatus = null;
            Guid? contractId = null;

            if (rfq.Status == Common.AWARDED_STATUS)
            {
                try
                {
                    var contract = await _buyerApiClient.GetSupplierContractStatus(
                        request.RFQId,
                        supplier.Id,
                        cancellationToken);

                    if (contract.ContractCreated)
                    {
                        contractStatus = contract.ContractStatus;
                        contractId = contract.ContractId;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Unable to fetch contract status for BuyerRFQId: {request.RFQId}, " +
                        $"SupplierId: {supplier.Id}. {ex.Message}");
                }
            }

            List<InvitedUserDto>? invitedUsers = null;

            if (request.RoleId.Equals(Common.SUPPLIER_ADMIN_ROLE_ID))
            {
                var invitedUserMappings = await _repositorywrapper.RFQOrganizationUserMapping
                    .FindByCondition(x => x.SupplierRFQId == supplierRFQId && x.IsActive)
                    .ToListAsync(cancellationToken);

                if (invitedUserMappings.Any())
                {
                    var userIds = invitedUserMappings
                        .Select(x => x.UserId)
                        .Distinct()
                        .ToList();

                    var identityUsers = await _identityApiClient.GetUsersByIds(userIds, cancellationToken);

                    invitedUsers = invitedUserMappings
                        .Select(x =>
                        {
                            var identity = identityUsers.FirstOrDefault(u => u.UserId == x.UserId);
                            return new InvitedUserDto
                            {
                                RFQId = x.BuyerRFQId,
                                SupplierId = x.SupplierId,
                                OrganizationId = x.OrganizationId,
                                UserId = x.UserId,
                                Name = identity?.Name,
                                Email = identity?.Email,
                                UserName = identity?.UserName
                            };
                        })
                        .ToList();
                }
            }


            return new GetRFQByIdDto
            {
                BuyerId = rfq.BuyerId,
                BuyerName = buyerName?.BuyerName,
                SupplierId = rfq.SupplierId,
                IsSupplierInvitedForContract = rfq.IsSupplierInvitedForContract,
                Title = rfq.Title,
                Description = rfq.Description,
                DeliveryLocation = rfq.DeliveryLocation,
                StartDate = rfq.StartDate,
                EndDate = rfq.EndDate,
                AddLotOption = rfq.AddLotOption,
                TechnicalSpecificationDocuments = technicalDocuments,
                TermsConditionDocuments = termsDocuments,
                ESignDocuments = esignDocuments,
                BuyerESignDocuments = attachmentResponse.ESignDocuments,
                SupplierTermsAndCondition = rfq.TermsAndCondition,
                SupplierTermsConditionDocuments = supplierTermsConditionDocuments,
                BuyerTermsAndConditionAccepted = rfq.BuyerTermsAndConditionAccepted ?? Common.PENDING,
                SupplierTermsAndConditionAccepted = supplierTermsAndConditionAccepted,
                Status = rfq.Status,
                ContractStatus = contractStatus,
                ContractId = contractId,
                Items = items,
                Questions = questions,
                SupplierQuotation = quotation == null
        ? new List<GetSupplierQuotationDto>()
        : new List<GetSupplierQuotationDto>
        {
            new GetSupplierQuotationDto
            {
                TotalPrice = quotation.TotalPrice,
                DeliveryCharge = quotation.DeliveryCharge,
                Tax = quotation.Tax,
                Discount = quotation.Discount,
                DeliveryType = quotation.DeliveryType,
                Status = quotation.Status,
                QutationId=quotation.Id,
                 IsLead = quotation.Id == lowestQuotation?.Id
            }
        },

                SupplierQuotationItems = quotationItems,
                InvitedUsers = invitedUsers,

            };
        }
    }
}