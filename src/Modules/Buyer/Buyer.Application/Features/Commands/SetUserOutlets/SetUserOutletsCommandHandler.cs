using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SetUserOutlets
{
    public class SetUserOutletsCommandHandler : IRequestHandler<SetUserOutletsCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIdentityApiClient _identityApiClient;

        public SetUserOutletsCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIdentityApiClient identityApiClient)
        {
            _repository = repository;
            _logger = logger;
            _identityApiClient = identityApiClient;
        }

        public async Task<Unit> Handle(SetUserOutletsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Assigning outlets to user. UserId: {request.UserId}, OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            // The user must belong to the signed-in organization.
            List<IdentityUserDto> organizationUsers = await _identityApiClient.GetOrganizationUsers(
                request.OrganizationId, cancellationToken);
            if (!organizationUsers.Any(user => user.UserId == request.UserId))
            {
                _logger.LogError($"User not found in organization. UserId: {request.UserId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("User not found.", "The user does not belong to this buyer organization.");
            }

            List<Guid> requestedOutletIds = (request.Request.OutletIds ?? new List<Guid>()).Distinct().ToList();
            List<Guid> buyerOutletIds = (await _repository.Wishlist.ListOutletsAsync(buyer.Id, cancellationToken))
                .Select(outlet => outlet.Id)
                .ToList();
            if (requestedOutletIds.Any(outletId => !buyerOutletIds.Contains(outletId)))
            {
                throw new NotFoundCustomException("Outlet not found.", "Select outlets that belong to this buyer organization.");
            }

            List<BuyerOutletUserMapping> existing = await _repository.BuyerOutletUserMapping
                .FindByCondition(x => x.UserId == request.UserId && buyerOutletIds.Contains(x.OutletId) && x.IsActive)
                .ToListAsync(cancellationToken);
            _repository.BuyerOutletUserMapping.DeleteRange(existing.Where(x => !requestedOutletIds.Contains(x.OutletId)));
            foreach (Guid outletId in requestedOutletIds.Where(id => !existing.Any(x => x.OutletId == id)))
            {
                _repository.BuyerOutletUserMapping.Create(new BuyerOutletUserMapping
                {
                    Id = Guid.NewGuid(),
                    OutletId = outletId,
                    UserId = request.UserId
                });
            }

            await _repository.SaveAsync();

            _logger.LogInfo($"Outlets assigned. UserId: {request.UserId}, Count: {requestedOutletIds.Count}");
            return Unit.Value;
        }
    }
}
