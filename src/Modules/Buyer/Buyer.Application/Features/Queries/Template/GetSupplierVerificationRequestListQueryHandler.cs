using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.SupplierVerification
{
    public class GetSupplierVerificationRequestListQueryHandler
        : IRequestHandler<GetSupplierVerificationRequestListQuery,
            List<SupplierVerificationRequestListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public GetSupplierVerificationRequestListQueryHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _logger = logger;
        }

        public async Task<List<SupplierVerificationRequestListDto>> Handle(
            GetSupplierVerificationRequestListQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching supplier verification requests for BuyerOrganizationId: {request.BuyerOrganizationId}, Index: {request.Index}, Limit: {request.Limit}");
            var requests = await _repository.SupplierVerificationRequest
                .FindByCondition(x =>
                    x.BuyerOrganizationId == request.BuyerOrganizationId &&
                    x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);
                _logger.LogInfo($"Fetched {requests.Count} supplier verification requests for BuyerOrganizationId: {request.BuyerOrganizationId}");

            var result = new List<SupplierVerificationRequestListDto>();

            foreach (var item in requests)
            {
                var supplier = await _supplierApiClient.GetSupplierById(
                    item.SupplierOrganizationId,
                    cancellationToken);

                string templateName = string.Empty;

                var template = await _repository.VerificationTemplate
                    .FindByCondition(x => x.Id == item.RFQVerificationTemplateId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (template != null)
                {
                    templateName = template.TemplateName;
                }
                else
                {
                    var defaultTemplate = await _repository.DefaultVerificationTemplateRepository
                        .FindByCondition(x => x.Id == item.RFQVerificationTemplateId)
                        .FirstOrDefaultAsync(cancellationToken);
                        _logger.LogInfo($"Fetched default template for templateId: {item.RFQVerificationTemplateId}");

                    if (defaultTemplate != null)
                    {
                        templateName = defaultTemplate.TemplateName;
                    }
                }

                result.Add(new SupplierVerificationRequestListDto
                {
                    RequestId = item.Id,
                    RFQNumber = item.RFQNumber,
                    SupplierOrganizationId = item.SupplierOrganizationId,
                    SupplierName = supplier.BusinessProfile.OrganizationName,
                    TemplateName = templateName,
                    Status = item.Status,
                    DueDate = item.DueDate,
                    DateCreated = item.DateCreated
                });
            }

            return result;
        }
    }
}