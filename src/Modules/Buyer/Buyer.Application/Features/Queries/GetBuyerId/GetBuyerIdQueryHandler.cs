using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Profile.Queries.GetBuyerId
{
    public class GetBuyerIdQueryHandler
        : IRequestHandler<GetBuyerIdQuery, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetBuyerIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<Guid> Handle(
            GetBuyerIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Buyer Id for Organization: {request.OrganizationId}");

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer profile does not exist.");
            }

            return Task.FromResult(buyer.Id);
        }
    }
}