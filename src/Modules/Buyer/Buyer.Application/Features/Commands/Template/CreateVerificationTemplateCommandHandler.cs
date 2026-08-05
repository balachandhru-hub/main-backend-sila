using System.Net.Http.Json;
using Buyer.Application.Features.StatusUpdate.Commands;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using SharedKernel.LoggerServices;
using Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Buyer.Application.Features.Commands.Template;

namespace Buyer.Application.Features.Commands.UpdateBuyerStatus
{
    public class CreateVerificationTemplateCommandHandler
        : IRequestHandler<CreateVerificationTemplateCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateVerificationTemplateCommandHandler(
            IRepositoryWrapper repositoryWrapper,
            HttpClient httpClient,
            IConfiguration configuration,
            ILoggerManager logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repositoryWrapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<Guid> Handle(
    CreateVerificationTemplateCommand request,
    CancellationToken cancellationToken)
        {
            _logger.LogInfo("Creating verification template.");

            BuyerBusinessProfile buyer = await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);

            _logger.LogInfo($"BuyerId fetched successfully: {buyer.Id} for OrganizationId: {request.OrganizationId}");

            var templateCount = await _repository.VerificationTemplate
                .FindByCondition(x => x.BuyerId == buyer.Id)
                .CountAsync(cancellationToken);
            _logger.LogInfo($"Existing template count for BuyerId {buyer.Id}: {templateCount}");

            var template = new VerificationTemplate
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                TemplateCode = $"TMP{(templateCount + 1):000}",
                TemplateName = request.VerificationTemplateDto.TemplateName,
                Description = request.VerificationTemplateDto.Description,
                Category = request.VerificationTemplateDto.Category,
            };

            _repository.VerificationTemplate.Create(template);
            _repository.Save();

            _logger.LogInfo($"Verification template created successfully. TemplateId: {template.Id}");

            return template.Id;
        }
    }
}