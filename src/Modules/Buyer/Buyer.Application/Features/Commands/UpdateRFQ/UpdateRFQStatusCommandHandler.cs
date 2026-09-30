using Buyer.Application.Contracts;
using Buyer.Application.Features.Commands.UpdateRFQ;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Handlers.UpdateRFQ
{
    public class UpdateRFQStatusCommandHandler
        : IRequestHandler<UpdateRFQStatusCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;

        public UpdateRFQStatusCommandHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<bool> Handle(
            UpdateRFQStatusCommand request,
            CancellationToken cancellationToken)
        {
            var rfq = await _repository.RFQ
                .FindByCondition(x =>
                    x.Id == request.Request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                
                    throw new NotFoundCustomException(
                        "RFQ not found.",
                        $"No RFQ was found with RFQId: {request.Request.RFQId}");
            
            }

            // 1. Update Buyer RFQ status
            rfq.Status = request.Request.Status;

            _repository.RFQ.Update(rfq);

            await _repository.SaveAsync();

            // 2. Update Supplier RFQ status
            await _supplierApiClient.UpdateSupplierRFQStatus(
                request.Request.RFQId,
                request.Request.Status,
                cancellationToken);

            return true;
        }
    }
}