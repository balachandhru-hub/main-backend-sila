using MediatR;
using Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using Identity.Application.Contracts;
using Identity.Domain.Common;
using Identity.Domain.Dto;
using Identity.Domain.Enum;
using Microsoft.AspNetCore.Http;

namespace Identity.Application.Features.Commands.OrganizationStatus
{
    public class EnableDisableOrganizationCommandHandler
        : IRequestHandler<EnableDisableOrganizationCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EnableDisableOrganizationCommandHandler(
            IRepositoryWrapper repository, IBuyerApiClient buyerApiClient,
    ISupplierApiClient supplierApiClient, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _buyerApiClient = buyerApiClient;
            _supplierApiClient = supplierApiClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> Handle(
            EnableDisableOrganizationCommand request,
            CancellationToken cancellationToken)
        {
            var organization = _repository.Organization
      .FindFirstByCondition(x => x.Id == request.OrganizationId);

            if (organization == null)
            {
                throw new NotFoundCustomException(
                    "Organization not found.",
                    "Organization does not exist.");
            }

            organization.IsActive = request.IsActive;

            
            _repository.Organization.Update(organization);

            var token = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (organization.OrganizationType == OrganizationType.Buyer)
            {
                await _buyerApiClient.UpdateStatus(
                    new UpdateOrganizationStatusDto
                    {
                        OrganizationId = organization.Id,
                        IsActive = request.IsActive
                    },
                    token,
                    cancellationToken);
            }
            else if (organization.OrganizationType == OrganizationType.Supplier)
            {
                await _supplierApiClient.UpdateStatus(
                    new UpdateOrganizationStatusDto
                    {
                        OrganizationId = organization.Id,
                        IsActive = request.IsActive
                    },
                    token,
                    cancellationToken);
            }


            await _repository.SaveAsync();

            return true;
        }
    }
}