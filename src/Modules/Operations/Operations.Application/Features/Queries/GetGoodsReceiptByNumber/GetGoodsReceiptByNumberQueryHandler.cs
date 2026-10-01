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

namespace Operations.Application.Features.Queries.GetGoodsReceiptByNumber
{
    public class GetGoodsReceiptByNumberQueryHandler : IRequestHandler<GetGoodsReceiptByNumberQuery, GoodsReceiptResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetGoodsReceiptByNumberQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GoodsReceiptResponseDto> Handle(GetGoodsReceiptByNumberQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching goods receipt by number. GrnNumber: {request.GrnNumber}, OrganizationId: {request.OrganizationId}");

            GoodsReceipt? receipt = await _repository.GoodsReceipt
                .FindByCondition(x => x.GrnNumber == request.GrnNumber && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (receipt == null)
            {
                _logger.LogError($"Goods receipt not found. GrnNumber: {request.GrnNumber}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Goods receipt not found.", "The goods receipt does not exist in your organization.");
            }

            GoodsReceiptResponseDto result = await ResponseBuilder.GoodsReceiptAsync(_repository, receipt, cancellationToken);
            _logger.LogInfo($"Goods receipt fetched. GoodsReceiptId: {receipt.Id}");
            return result;
        }
    }
}
