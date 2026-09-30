using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.RFQAttachment
{
    public class InviteSupplierForContractCommandHandler
        : IRequestHandler<InviteSupplierForContractCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public InviteSupplierForContractCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            InviteSupplierForContractCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Inviting supplier for contract. BuyerRFQId: {request.RFQId}, " +
                $"SupplierId: {request.SupplierId}");

            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.SupplierId == request.SupplierId)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
            {
                _logger.LogError(
                    $"RFQ not found for BuyerRFQId: {request.RFQId} and SupplierId: {request.SupplierId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with Id: {request.RFQId} for SupplierId: {request.SupplierId}.");
            }

            supplierRFQ.IsSupplierInvitedForContract = true;
            _repository.SupplierRFQ.Update(supplierRFQ);
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier invited for contract. BuyerRFQId: {request.RFQId}, " +
                $"SupplierId: {request.SupplierId}");

            return supplierRFQ.Id;
        }
    }
}
