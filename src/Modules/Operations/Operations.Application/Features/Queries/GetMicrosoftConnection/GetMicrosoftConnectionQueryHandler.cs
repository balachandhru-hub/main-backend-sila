using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetMicrosoftConnection
{
    public class GetMicrosoftConnectionQueryHandler : IRequestHandler<GetMicrosoftConnectionQuery, MicrosoftConnectionValidationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetMicrosoftConnectionQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<MicrosoftConnectionValidationResponseDto> Handle(GetMicrosoftConnectionQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Microsoft connection. ConnectionId: {request.ConnectionId}, OrganizationId: {request.OrganizationId}");

            DocumentStorageConnection connection = await MicrosoftStorageWorkflow.GetConnectionAsync(_repository, _logger, request.ConnectionId, request.OrganizationId);
            _logger.LogInfo($"Microsoft connection fetched. ConnectionId: {connection.Id}, Status: {connection.ConnectionStatus}");
            return MicrosoftStorageWorkflow.ToValidationResponse(connection, connection.ConnectionStatus == StorageConnectionStatus.CONNECTED
                ? "Microsoft SharePoint read/write access is validated."
                : "Microsoft SharePoint still requires destination validation.");
        }
    }
}
