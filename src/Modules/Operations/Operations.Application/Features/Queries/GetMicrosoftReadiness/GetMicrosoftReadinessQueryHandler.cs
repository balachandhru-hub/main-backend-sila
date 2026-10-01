using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Graph;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetMicrosoftReadiness
{
    public class GetMicrosoftReadinessQueryHandler : IRequestHandler<GetMicrosoftReadinessQuery, MicrosoftReadinessResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMicrosoftGraphClient _graph;

        public GetMicrosoftReadinessQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMicrosoftGraphClient graph)
        {
            _repository = repository;
            _logger = logger;
            _graph = graph;
        }

        public async Task<MicrosoftReadinessResponseDto> Handle(GetMicrosoftReadinessQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Microsoft readiness. OrganizationId: {request.OrganizationId}");
            return await Task.FromResult(_graph.GetReadiness());
        }
    }
}
