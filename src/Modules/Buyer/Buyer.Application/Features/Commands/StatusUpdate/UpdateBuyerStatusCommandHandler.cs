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
                .FindFirstByCondition(x => x.Id == request.BuyerId && x.IsActive);

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer not found.");
            }

            var registrations = _repositoryWrapper.BuyerRegistration
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToList();

            if (request.Status.Equals(Common.REJECTED_STATUS, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(request.Comments))
            {
                throw new NotFoundCustomException(
                    "Comments are mandatory when rejecting a buyer.",
                    "Please provide rejection comments.");
            }

            buyer.Status = request.Status;
            buyer.Comment = request.Comments;

            // If Approved, verify all registrations
            if (request.Status.Equals(Common.VERIFIED_STATUS, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var registration in registrations)
                {
                    registration.IsVerified = true;
                    registration.VerifiedOn = DateTime.UtcNow;

                    _repositoryWrapper.BuyerRegistration.Update(registration);
                }
            }



            _repositoryWrapper.BuyerBusinessProfile.Update(buyer);

            await _repositoryWrapper.SaveAsync();

            return true;
        }
    }
}