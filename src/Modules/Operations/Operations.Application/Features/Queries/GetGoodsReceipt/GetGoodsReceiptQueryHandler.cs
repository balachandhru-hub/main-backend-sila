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

namespace Operations.Application.Features.Queries.GetGoodsReceipt
{
    public class GetGoodsReceiptQueryHandler : IRequestHandler<GetGoodsReceiptQuery, GoodsReceiptResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetGoodsReceiptQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GoodsReceiptResponseDto> Handle(GetGoodsReceiptQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching goods receipt. GoodsReceiptId: {request.GoodsReceiptId}, OrganizationId: {request.OrganizationId}");

            GoodsReceipt? receipt = await _repository.GoodsReceipt
                .FindByCondition(x => x.Id == request.GoodsReceiptId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (receipt == null)
            {
                _logger.LogError($"Goods receipt not found. GoodsReceiptId: {request.GoodsReceiptId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Goods receipt not found.", "The goods receipt does not exist in your organization.");
            }

            GoodsReceiptResponseDto result = await ResponseBuilder.GoodsReceiptAsync(_repository, receipt, cancellationToken);
            _logger.LogInfo($"Goods receipt fetched. GoodsReceiptId: {receipt.Id}");
            return result;
        }
    }
}
