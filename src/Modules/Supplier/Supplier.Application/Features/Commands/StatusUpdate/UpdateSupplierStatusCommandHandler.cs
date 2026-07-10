using System.Net.Http.Json;
using Supplier.Application.Features.StatusUpdate.Commands;
using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.UpdateSupplierStatus
{
    public class UpdateSupplierStatusCommandHandler
        : IRequestHandler<UpdateSupplierStatusCommand, bool>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UpdateSupplierStatusCommandHandler> _logger;

        public UpdateSupplierStatusCommandHandler(
            IRepositoryWrapper repositoryWrapper,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<UpdateSupplierStatusCommandHandler> logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<bool> Handle(
     UpdateSupplierStatusCommand request,
     CancellationToken cancellationToken)
        {
            var supplier = _repositoryWrapper.SupplierBusinessProfile
                .FindFirstByCondition(x => x.Id == request.SupplierId && x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    "Supplier not found.");
            }

            var registrations = _repositoryWrapper.SupplierRegistration
                .FindByCondition(x => x.SupplierId == supplier.Id && x.IsActive)
                .ToList();

            if (request.Status.Equals(Common.REJECTED_STATUS, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(request.Comments))
            {
                throw new NotFoundCustomException(
                    "Comments are mandatory when rejecting a supplier.",
                    "Please provide rejection comments.");
            }

            supplier.Status = request.Status;
            supplier.Comment = request.Comments;

            // If Approved, verify all registrations
            if (request.Status.Equals(Common.VERIFIED_STATUS, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var registration in registrations)
                {
                    registration.IsVerified = true;
                    registration.VerifiedOn = DateTime.UtcNow;

                    _repositoryWrapper.SupplierRegistration.Update(registration);
                }
            }



            _repositoryWrapper.SupplierBusinessProfile.Update(supplier);

            await _repositoryWrapper.SaveAsync();

            return true;
        }
    }
}