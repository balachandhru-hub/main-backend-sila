using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.InviteSuppliers
{
    public class InviteSuppliersCommandHandler
        : IRequestHandler<InviteSuppliersCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;

        public InviteSuppliersCommandHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            InviteSuppliersCommand request,
            CancellationToken cancellationToken)
        {
           
           

            foreach (var supplierId in request.Invite.SupplierInvites)
{
    await _repository.SupplierVerificationRequest.CreateAsync(
        new SupplierVerificationRequest
        {
            Id = Guid.NewGuid(),
            RFQId = request.Invite.RFQId,
            RFQNumber = request.Invite.RFQNumber,
            BuyerOrganizationId = request.Invite.BuyerOrganizationId,
            SupplierOrganizationId = supplierId,
            RFQVerificationTemplateId =
                request.Invite.RFQVerificationTemplateId,
            Status = Common.PENDING
        });
}

return true;

          

           
        }
    }
}