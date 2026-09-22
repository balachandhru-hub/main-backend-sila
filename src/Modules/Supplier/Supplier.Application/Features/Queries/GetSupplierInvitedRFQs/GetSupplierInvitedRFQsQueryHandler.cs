using Supplier.Application.Contracts;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.GetSupplierInvitedRFQs
{
    public class GetSupplierInvitedRFQsQueryHandler
        : IRequestHandler<GetSupplierInvitedRFQsQuery, List<SupplierInvitedRFQContractDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly ILoggerManager _logger;

        public GetSupplierInvitedRFQsQueryHandler(
            IRepositoryWrapper repository,
            IBuyerApiClient buyerApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _buyerApiClient = buyerApiClient;
            _logger = logger;
        }

        public async Task<List<SupplierInvitedRFQContractDto>> Handle(
            GetSupplierInvitedRFQsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching invited RFQs with contract status. " +
                $"OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    "Supplier does not exist.");
            }

            var query = _repository.SupplierRFQ
                .FindByCondition(x => x.SupplierId == supplier.Id);

            if (!request.RoleId.Equals(Common.SUPPLIER_ADMIN_ROLE_ID))
            {
                // Regular supplier user: only the RFQs THIS user was invited to,
                // matched by UserId in RFQOrganizationUserMapping. Supplier admins
                // bypass this and see every RFQ (and therefore every contract)
                // belonging to the supplier organization.
                var invitedSupplierRFQIds = _repository.RFQOrganizationUserMapping
                    .FindByCondition(x =>
                        x.SupplierId == supplier.Id &&
                        x.UserId == request.UserId &&
                        x.IsActive)
                    .Select(x => x.SupplierRFQId);

                query = query.Where(x => invitedSupplierRFQIds.Contains(x.Id));
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(x =>
                    x.RFQNumber.Contains(search) ||
                    x.Title.Contains(search));
            }

            var rfqs = await query
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            var result = new List<SupplierInvitedRFQContractDto>();

            foreach (var rfq in rfqs)
            {
                bool contractCreated = false;
                Guid? contractId = null;
                string? contractNumber = null;
                string? contractStatus = null;

                // Always looked up by (rfqId, THIS supplier.Id) so a supplier can
                // never see another supplier's contract for the same RFQ.
                try
                {
                    var contract = await _buyerApiClient.GetSupplierContractStatus(
                        rfq.BuyerRFQId,
                        supplier.Id,
                        cancellationToken);

                    if (contract.ContractCreated)
                    {
                        contractCreated = true;
                        contractId = contract.ContractId;
                        contractNumber = contract.ContractNumber;
                        contractStatus = contract.ContractStatus;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Unable to fetch contract status for BuyerRFQId: {rfq.BuyerRFQId}, " +
                        $"SupplierId: {supplier.Id}. {ex.Message}");
                }

                // Skip RFQs that don't have a contract for this supplier yet.
                if (!contractCreated)
                {
                    continue;
                }

                result.Add(new SupplierInvitedRFQContractDto
                {
                    RFQId = rfq.BuyerRFQId,
                    SupplierRFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    Title = rfq.Title,
                    Status = rfq.Status,
                    EndDate = rfq.EndDate,
                    DeliveryLocation = rfq.DeliveryLocation,
                    BuyerName = rfq.BuyerName,
                    ContractCreated = contractCreated,
                    ContractId = contractId,
                    ContractNumber = contractNumber,
                    ContractStatus = contractStatus
                });
            }

            return result;
        }
    }
}
