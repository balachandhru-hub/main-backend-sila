using System.Net.Http.Json;
using Buyer.Application.Features.StatusUpdate.Commands;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Commands.UpdateBuyerStatus
{
    public class UpdateBuyerStatusCommandHandler
        : IRequestHandler<UpdateBuyerStatusCommand, bool>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UpdateBuyerStatusCommandHandler> _logger;

        public UpdateBuyerStatusCommandHandler(
            IRepositoryWrapper repositoryWrapper,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<UpdateBuyerStatusCommandHandler> logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<bool> Handle(
    UpdateBuyerStatusCommand request,
    CancellationToken cancellationToken)
        {
            var buyer = _repositoryWrapper.BuyerBusinessProfile
                .FindFirstByCondition(x => x.OrganizationId == request.OrganizationId && x.IsActive);

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer not found.");
            }

            string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;

            var response = await _httpClient.PostAsJsonAsync(
                $"{masterDataUrl}/api/v1/metadata/reference-list",
                new List<string> { Common.METADATA_STATUS_TYPE },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new PreConditionFailedCustomException(
                    "Unable to fetch Buyer Status metadata.",
                    "Unable to fetch Buyer Status metadata.");
            }

            var metadataList = await response.Content.ReadFromJsonAsync<List<MetadataDto>>(
                cancellationToken: cancellationToken);

            if (metadataList == null || !metadataList.Any())
            {
                throw new NotFoundCustomException(
                    "Buyer Status metadata not found.",
                    "Buyer Status metadata not found.");
            }

            // Validate status against MasterData
            var statusExists = metadataList.Any(x =>
                x.Key.Equals(request.Status, StringComparison.OrdinalIgnoreCase));

            if (!statusExists)
            {
                throw new NotFoundCustomException(
                    "Invalid Buyer Status.",
                    $"'{request.Status}' is not a valid Buyer Status.");
            }

            if (request.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(request.Comments))
            {
                throw new NotFoundCustomException(
                    "Comments are mandatory when rejecting a buyer.",
                    "Please provide rejection comments.");
            }

            buyer.Status = request.Status;
            buyer.Comment = request.Comments;

            _repositoryWrapper.BuyerBusinessProfile.Update(buyer);
            await _repositoryWrapper.SaveAsync();

            return true;
        }
    }
}