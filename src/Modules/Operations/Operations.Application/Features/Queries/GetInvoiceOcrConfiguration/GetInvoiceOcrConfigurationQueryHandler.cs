using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetInvoiceOcrConfiguration
{
    public class GetInvoiceOcrConfigurationQueryHandler : IRequestHandler<GetInvoiceOcrConfigurationQuery, InvoiceOcrConfigurationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetInvoiceOcrConfigurationQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceOcrConfigurationResponseDto> Handle(GetInvoiceOcrConfigurationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching invoice OCR configuration. OrganizationId: {request.OrganizationId}");

            InvoiceOcrConfiguration configuration = await InvoiceExtractionWorkflow.OcrConfigurationAsync(_repository, request.OrganizationId, cancellationToken);
            _logger.LogInfo($"Invoice OCR configuration fetched. OrganizationId: {request.OrganizationId}, Version: {configuration.Version}");
            return ResponseBuilder.OcrConfiguration(configuration);
        }
    }
}
