using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.SupplierVerification
{
    public class GetSupplierVerificationRequestBySupplierIdQueryHandler
        : IRequestHandler<GetSupplierVerificationRequestBySupplierIdQuery,
            List<SupplierVerificationRequestListBySupplierIdDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public GetSupplierVerificationRequestBySupplierIdQueryHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _logger = logger;
        }

        public async Task<List<SupplierVerificationRequestListBySupplierIdDto>> Handle(
            GetSupplierVerificationRequestBySupplierIdQuery request,
            CancellationToken cancellationToken)
        {
            var requests = await _repository.SupplierVerificationRequest
                .FindByCondition(x =>
                    x.SupplierOrganizationId == request.SupplierOrganizationId &&
                    x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            var result = new List<SupplierVerificationRequestListBySupplierIdDto>();

            foreach (var item in requests)
            {
                var supplier = await _supplierApiClient.GetSupplierById(
                    item.SupplierOrganizationId,
                    cancellationToken);

                string templateName = string.Empty;

                var template = await _repository.VerificationTemplate
                    .FindByCondition(x => x.Id == item.RFQVerificationTemplateId)
                    .FirstOrDefaultAsync(cancellationToken);
                    _logger.LogInfo($"Fetched template for templateId: {item.RFQVerificationTemplateId}");

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

                var questionEntities = await _repository.VerificationTemplateQuestion
                    .FindByCondition(x =>
                        x.VerificationTemplateId == item.RFQVerificationTemplateId)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(cancellationToken);
                _logger.LogInfo($"Fetched {questionEntities.Count} questions for templateId: {item.RFQVerificationTemplateId}");

                var questionIds = questionEntities
                    .Select(x => x.Id)
                    .ToList();

               
                var optionEntities = await _repository.VerificationTemplateQuestionOptionRepository
                    .FindByCondition(x => questionIds.Contains(x.VerificationTemplateQuestionId))
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(cancellationToken);
                    _logger.LogInfo($"Fetched {optionEntities.Count} options for questions related to templateId: {item.RFQVerificationTemplateId}");

              
                var questions = questionEntities
                    .Select(x => new SupplierVerificationQuestionDto
                    {
                        VerificationTemplateQuestionId = x.Id,
                        Question = x.Question,
                        QuestionType = x.QuestionType,
                        IsRequired = x.IsRequired,
                        DisplayOrder = x.DisplayOrder,

                        Options = optionEntities
                            .Where(o => o.VerificationTemplateQuestionId == x.Id)
                            .OrderBy(o => o.DisplayOrder)
                            .Select(o => new SupplierVerificationQuestionOptionDto
                            {
                                Id = o.Id,
                                OptionText = o.OptionText,
                                DisplayOrder = o.DisplayOrder
                            })
                            .ToList()
                    })
                    .ToList();

                result.Add(new SupplierVerificationRequestListBySupplierIdDto
                {
                    RequestId = item.Id,
                    RFQNumber = item.RFQNumber,
                    SupplierOrganizationId = item.SupplierOrganizationId,
                    SupplierName = supplier.BusinessProfile.OrganizationName,
                    BuyerOrganizationId = item.BuyerOrganizationId,
                    TemplateName = templateName,
                    Status = item.Status,
                    DueDate = item.DueDate,
                    DateCreated = item.DateCreated,
                    Questions = questions
                });
            }

            return result;
        }
    }
}