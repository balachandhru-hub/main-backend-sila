using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
            IRepositoryWrapper repository,
            ILoggerManager logger)
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

            string status = Common.PENDING;

            var template = _repository.VerificationTemplate
                .FindFirstByCondition(x =>
                    x.Id == request.Invite.RFQVerificationTemplateId &&
                    x.IsActive);

            if (template != null &&
                (
                    template.TemplateCode == Common.DEFAULT_TEMPLATE
                    
                ))
            {
                status = Common.DEFAULT;
            }

            foreach (var supplierId in request.Invite.SupplierInvites)
            {
                await _repository.SupplierVerificationRequest.CreateAsync(
                    new SupplierVerificationRequest
                    {
                        Id = Guid.NewGuid(),
                        RFQId = request.Invite.RFQId,
                        RFQNumber = request.Invite.RFQNumber,
                        BuyerOrganizationId = request.Invite.BuyerId,
                        SupplierOrganizationId = supplierId,
                        RFQVerificationTemplateId = request.Invite.RFQVerificationTemplateId,
                        Status = status,
                        DueDate = request.Invite.EndDate
                    });
            }

            _logger.LogInfo(
                $"Supplier verification requests created successfully. Status: {status}, Total Suppliers: {request.Invite.SupplierInvites.Count}");

            return request.Invite.RFQId;
        }
    }
}