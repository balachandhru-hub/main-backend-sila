using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.Template
{
    public class CreateVerificationDefaultTemplateCommandHandler
        : IRequestHandler<CreateVerificationDefaultTemplateCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateVerificationDefaultTemplateCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateVerificationDefaultTemplateCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Creating Default Verification Template.");

            // Get Buyer Business Profile using OrganizationId
            var buyer = await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    $"Buyer profile not found for OrganizationId : {request.OrganizationId}");
            }

            // Check if default template already exists
            var template = await _repository.VerificationTemplate
                .FindByCondition(x =>
                    x.BuyerId == buyer.Id &&
                    x.TemplateCode == "TMP001")
                .FirstOrDefaultAsync(cancellationToken);

            if (template != null)
            {
                _logger.LogInfo("Default template already exists.");

                return template.Id;
            }

            var verificationTemplate = new VerificationTemplate
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id, // BuyerBusinessProfile.Id
                TemplateCode = "TMP001",
                TemplateName = "Default Supplier Profile",
                Description = "Default supplier registration details."
            };

            _repository.VerificationTemplate.Create(verificationTemplate);

            var saved = await _repository.SaveAsync();

            if (!saved)
            {
                throw new BadRequestCustomException(
                    "Unable to create template.",
                    "Database save failed.");
            }

            _logger.LogInfo($"Default Template Created : {verificationTemplate.Id}");

            return verificationTemplate.Id;
        }
    }
}