using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetBuyerNameById
{
    public class GetBuyerNameByIdQueryHandler
        : IRequestHandler<GetBuyerNameByIdQuery, BuyerNameDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetBuyerNameByIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<BuyerNameDto> Handle(
            GetBuyerNameByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Buyer Name for BuyerId: {request.BuyerId}");

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.Id == request.BuyerId &&
                    x.IsActive);

            if (buyer == null)
            {
                _logger.LogError(
                    $"Buyer not found for BuyerId: {request.BuyerId}");
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer profile does not exist.");
            }

            return Task.FromResult(new BuyerNameDto
            {
                BuyerId = buyer.Id,
                BuyerName = buyer.OrganizationName
            });
        }
    }
}
