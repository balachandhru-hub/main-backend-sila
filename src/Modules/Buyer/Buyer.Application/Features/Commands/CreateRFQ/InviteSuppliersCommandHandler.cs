using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.InviteSuppliers
{
    public class InviteSuppliersCommandHandler
        : IRequestHandler<InviteSuppliersCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public InviteSuppliersCommandHandler(
            IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            InviteSuppliersCommand request,
            CancellationToken cancellationToken)
        {

            _logger.LogInfo(
                            $"Supplier invitation process started. RFQ Id: {request.Invite.RFQId}, RFQ Number: {request.Invite.RFQNumber}");

            foreach (var supplierId in request.Invite.SupplierInvites)
            {
                _logger.LogError(
                    $"No suppliers found for invitation. RFQ Id: {request.Invite.RFQId}");

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
                _logger.LogInfo($"Supplier verification requests created successfully. Total Suppliers: {request.Invite.SupplierInvites.Count}, RFQ Id: {request.Invite.RFQId}");
            }

            return request.Invite.RFQId; ;




        }
    }
}