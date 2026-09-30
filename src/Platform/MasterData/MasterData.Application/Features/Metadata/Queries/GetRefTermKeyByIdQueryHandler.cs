using MasterData.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.Metadata.Queries.GetRefTermKeyById
{
    public class GetRefTermKeyByIdQueryHandler
        : IRequestHandler<GetRefTermKeyByIdQuery, string>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetRefTermKeyByIdQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager loggerManager)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = loggerManager;
        }

        public async Task<string> Handle(
            GetRefTermKeyByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching reference term key for Id: {request.Id}");

            var metadata = _repositoryWrapper.Metadata
                .FindFirstByCondition(x => x.IsActive && x.Id == request.Id);

            if (metadata == null)
            {
                _logger.LogError($"Reference term not found for Id: {request.Id}");

                throw new NotFoundCustomException(
                    "Reference term not found.",
                    $"No reference term found for Id: {request.Id}");
            }

            return await Task.FromResult(metadata.Key);
        }
    }
}