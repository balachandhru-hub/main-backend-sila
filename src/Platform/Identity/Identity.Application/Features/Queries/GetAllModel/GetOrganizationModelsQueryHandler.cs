using Contracts.IRepository;
using Identity.Domain.Dto;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Identity.Domain.Common;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Queries.GetAllModel
{
    public class GetOrganizationModelQueryHandler
        : IRequestHandler<GetOrganizationModelQuery, List<ModelDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILoggerManager _logger;

        public GetOrganizationModelQueryHandler(
            IRepositoryWrapper repository,
            IHttpContextAccessor httpContextAccessor,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<List<ModelDto>> Handle(
            GetOrganizationModelQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Fetching organization model for OrganizationId: " + request.OrganizationId);
            var role = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value);

            Guid organizationId;

            if (role == Common.PLATFORM_ADMINISTRATOR_ID)
            {
                _logger.LogInfo("User has PLATFORM_ADMINISTRATOR role. Using provided OrganizationId: " + request.OrganizationId);
                if (!request.OrganizationId.HasValue)
                {

                    _logger.LogError("OrganizationId is required for PLATFORM_ADMINISTRATOR role.");
                    throw new BadRequestCustomException("OrganizationId is required for PLATFORM_ADMINISTRATOR role.", "OrganizationId is required.");
                }

                organizationId = request.OrganizationId.Value;
            }
            else
            {
                _logger.LogInfo("User does not have PLATFORM_ADMINISTRATOR role. Fetching OrganizationId from token claims.");
                var claim = _httpContextAccessor.HttpContext?
                    .User?
                    .FindFirst("OrganizationId")?
                    .Value;

                if (!Guid.TryParse(claim, out organizationId))
                {
                    _logger.LogError("Organization claim not found or invalid.");
                    throw new UnAuthorizedCustomException(
                        "Unauthorized",
                        "Organization claim not found.");
                }

            }

            var models = (

                from mapping in _repository.OrganizationModelMapping.FindByConditionAsync(x =>
                    x.OrganizationId == organizationId)

                join model in _repository.ModelMapping.FindByConditionAsync(x => x.IsActive)
                    on mapping.ModelId equals model.Id

                select new ModelDto
                {
                    Id = model.Id,
                    Key = model.Key,
                    ModelName = model.ModelName
                })
                .ToList();
            _logger.LogInfo($"Fetched {models.Count} models for OrganizationId: " + organizationId);
            return await Task.FromResult(models);
        }
    }
}