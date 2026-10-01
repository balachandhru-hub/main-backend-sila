using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateOutlet
{
    public class CreateOutletCommandHandler : IRequestHandler<CreateOutletCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateOutletCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateOutletCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating outlet. OrganizationId: {request.OrganizationId}");

            if (string.IsNullOrWhiteSpace(request.Request.OutletName))
            {
                throw new BadRequestCustomException("Outlet name is required.", "Enter an outlet name.");
            }

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            await ValidateApprovalFlowAsync(_repository, _logger, request.Request.MasterApprovalFlowId, buyer.Id);

            BuyerOutlet outlet = new BuyerOutlet
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                OutletName = request.Request.OutletName.Trim(),
                OutletCode = request.Request.OutletCode,
                Description = request.Request.Description,
                ExternalShipTo = request.Request.ExternalShipTo,
                AddressLine1 = request.Request.AddressLine1,
                City = request.Request.City,
                Country = request.Request.Country,
                MasterApprovalFlowId = request.Request.MasterApprovalFlowId
            };
            _repository.BuyerOutlet.Create(outlet);
            await _repository.SaveAsync();

            _logger.LogInfo($"Outlet created. OutletId: {outlet.Id}, BuyerId: {buyer.Id}");
            return outlet.Id;
        }

        // An outlet's approval flow must be an active WISHLIST flow of the same buyer.
        internal static async Task ValidateApprovalFlowAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid? masterApprovalFlowId,
            Guid buyerId)
        {
            if (masterApprovalFlowId == null || masterApprovalFlowId == Guid.Empty)
            {
                return;
            }

            MasterApprovalFlow? flow = await repository.MasterApprovalFlow.FindFirstByConditionAsync(
                x => x.Id == masterApprovalFlowId && x.BuyerId == buyerId && x.IsActive);
            if (flow == null)
            {
                logger.LogError($"Approval flow not found. ApprovalFlowId: {masterApprovalFlowId}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("Approval flow not found.", "The approval flow does not belong to this buyer organization.");
            }

            if (!string.Equals(flow.Type, Common.WISHLIST_APPROVAL_TYPE, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Approval flow type is not Wishlist.", "Select an approval configuration of type WISHLIST.");
            }
        }
    }
}
