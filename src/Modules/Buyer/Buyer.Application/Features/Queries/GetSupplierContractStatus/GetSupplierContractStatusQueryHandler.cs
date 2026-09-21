using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSupplierContractStatus
{
    public class GetSupplierContractStatusQueryHandler
        : IRequestHandler<GetSupplierContractStatusQuery, SupplierContractStatusDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierContractStatusQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierContractStatusDto> Handle(
            GetSupplierContractStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Checking contract for RFQId: {request.RFQId}, SupplierId: {request.SupplierId}");

            var contract = await _repository.Contract
                .FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.SupplierId == request.SupplierId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (contract == null)
            {
                return new SupplierContractStatusDto { ContractCreated = false };
            }

            return new SupplierContractStatusDto
            {
                ContractCreated = true,
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber,
                ContractStatus = contract.ContractStatus
            };
        }
    }
}
