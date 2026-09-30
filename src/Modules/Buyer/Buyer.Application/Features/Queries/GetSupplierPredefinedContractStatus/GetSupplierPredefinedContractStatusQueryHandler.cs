using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSupplierPredefinedContractStatus
{
    public class GetSupplierPredefinedContractStatusQueryHandler
        : IRequestHandler<GetSupplierPredefinedContractStatusQuery, SupplierPredefinedContractStatusDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierPredefinedContractStatusQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierPredefinedContractStatusDto> Handle(
            GetSupplierPredefinedContractStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Checking contract for RFQId: {request.RFQId}, SupplierId: {request.SupplierId}");

            var contract = await _repository.PredefinedContract
                .FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.SupplierId == request.SupplierId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (contract == null)
            {
                return new SupplierPredefinedContractStatusDto { ContractCreated = false };
            }

            return new SupplierPredefinedContractStatusDto
            {
                ContractCreated = true,
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber,
                ContractStatus = contract.ContractStatus
            };
        }
    }
}
