using Buyer.Application.Contracts;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.InviteSupplierForContract
{
    public class InviteSupplierForContractCommandHandler
        : IRequestHandler<InviteSupplierForContractCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public InviteSupplierForContractCommandHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            InviteSupplierForContractCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Inviting supplier for contract. RFQId: {request.RFQId}, " +
                $"SupplierId: {request.SupplierId}");

            var rfq = await _repository.RFQ
                .FindByCondition(x =>
                    x.Id == request.RFQId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. RFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {request.RFQId}");
            }

            await _supplierApiClient.InviteSupplierForContract(
                rfq.Id,
                request.SupplierId,
                cancellationToken);

            _logger.LogInfo(
                $"Supplier invited for contract successfully. RFQId: {rfq.Id}, " +
                $"SupplierId: {request.SupplierId}");

            return rfq.Id;
        }
    }
}
