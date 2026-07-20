using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public class UpdateItemBuyerMasterCommandHandler
        : IRequestHandler<UpdateItemBuyerMasterCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateItemBuyerMasterCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateItemBuyerMasterCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Updating Item Buyer Master.");

            var dto = request.ItemBuyerMaster;

            var entity = await _repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x =>
                    x.Id == dto.Id &&
                    x.IsActive);

            if (entity == null)
            {
                throw new NotFoundCustomException(
                    "Item not found.",
                    "Item Buyer Master not found.");
            }

            Guid buyerId;

            if (dto.BuyerId.HasValue)
            {
                buyerId = dto.BuyerId.Value;
            }
            else
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.OrganizationId == request.OrganizationId &&
                        x.IsActive);

                if (buyer == null)
                {
                    throw new NotFoundCustomException(
                        "Buyer not found.",
                        "Buyer profile not found.");
                }

                buyerId = buyer.Id;
            }

            var duplicate = await _repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x =>
                    x.MaterialCode == dto.MaterialCode &&
                    x.Id != dto.Id &&
                    x.IsActive);

            if (duplicate != null)
            {
                throw new PreConditionFailedCustomException(
                    "Material Code already exists.",
                    $"Material Code '{dto.MaterialCode}' already exists.");
            }

            entity.BuyerId = buyerId;
            entity.Description = dto.Description;
            entity.MaterialCode = dto.MaterialCode;
            entity.MaterialGroup = dto.MaterialGroup;

            _repository.ItemBuyerMaster.Update(entity);

            await _repository.SaveAsync();

            _logger.LogInfo($"ItemBuyerMaster updated : {entity.Id}");

            return true;
        }
    }
}