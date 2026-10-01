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

namespace Operations.Application.Features.Queries.GetGoodsReceipts
{
    public class GetGoodsReceiptsQueryHandler : IRequestHandler<GetGoodsReceiptsQuery, List<GoodsReceiptResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetGoodsReceiptsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<GoodsReceiptResponseDto>> Handle(GetGoodsReceiptsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching goods receipts. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            List<GoodsReceipt> receipts = await _repository.GoodsReceipt
                .FindByCondition(x => x.OrganizationId == request.OrganizationId)
                .OrderByDescending(x => x.DateCreated)
                .Take(200)
                .ToListAsync(cancellationToken);
            List<GoodsReceiptResponseDto> result = await ResponseBuilder.GoodsReceiptsAsync(_repository, receipts, cancellationToken);

            _logger.LogInfo($"Goods receipts fetched. Count: {result.Count}, OrganizationId: {request.OrganizationId}");
            return result;
        }
    }
}
