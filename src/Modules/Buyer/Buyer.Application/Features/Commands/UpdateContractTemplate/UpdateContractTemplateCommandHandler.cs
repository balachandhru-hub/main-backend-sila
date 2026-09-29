using Buyer.Application.Features.Assets.Commands;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateContractTemplate
{
    public class UpdateContractTemplateCommandHandler
        : IRequestHandler<UpdateContractTemplateCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public UpdateContractTemplateCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateContractTemplateCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            _logger.LogInfo(
                $"Updating contract template. Id: {request.Id}, BuyerId: {request.BuyerId}");

            var contractTemplate = await _repository.ContractTemplate
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.BuyerId == request.BuyerId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (contractTemplate == null)
            {
                _logger.LogError($"Contract template not found. Id: {request.Id}");
                throw new NotFoundCustomException(
                    "Contract template not found.",
                    $"No contract template was found with Id: {request.Id}");
            }

            contractTemplate.TemplateName = dto.TemplateName;

            if (dto.Attachment != null)
            {
                contractTemplate.AssetId = await _mediator.Send(
                    new UploadAssetCommand(dto.Attachment), cancellationToken);
            }

            _repository.ContractTemplate.Update(contractTemplate);
            await _repository.SaveAsync();

            _logger.LogInfo($"Contract template updated. Id: {contractTemplate.Id}");

            return contractTemplate.Id;
        }
    }
}
