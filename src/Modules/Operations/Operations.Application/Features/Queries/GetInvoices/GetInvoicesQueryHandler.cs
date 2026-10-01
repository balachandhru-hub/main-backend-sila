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

namespace Operations.Application.Features.Queries.GetInvoices
{
    public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, List<InvoiceResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetInvoicesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<InvoiceResponseDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching invoices. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            List<Invoice> invoices = await _repository.Invoice
                .FindByCondition(x => x.OrganizationId == request.OrganizationId)
                .OrderByDescending(x => x.DateCreated)
                .Take(200)
                .ToListAsync(cancellationToken);
            List<InvoiceResponseDto> result = await ResponseBuilder.InvoicesAsync(_repository, invoices, cancellationToken);

            _logger.LogInfo($"Invoices fetched. Count: {result.Count}, OrganizationId: {request.OrganizationId}");
            return result;
        }
    }
}
