using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public class DeleteItemBuyerMasterCommandHandler
        : IRequestHandler<DeleteItemBuyerMasterCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteItemBuyerMasterCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            DeleteItemBuyerMasterCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Deleting Item Buyer Master.");

            var entity = await _repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x =>
                    x.Id == request.Id &&
                    x.IsActive);

            if (entity == null)
            {
                throw new NotFoundCustomException(
                    "Item not found.",
                    "Item Buyer Master not found.");
            }

            entity.IsActive = false;

            _repository.ItemBuyerMaster.Update(entity);

            await _repository.SaveAsync();

            _logger.LogInfo($"ItemBuyerMaster deleted : {entity.Id}");

            return true;
        }
    }
}