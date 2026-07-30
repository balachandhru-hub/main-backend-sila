using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Identity;
using HashingSystem;
using SharedKernel.ExceptionHandler;
using Identity.Domain.Common;

namespace Identity.Application.Features.Commands.Register
{
    public class RegisterCommandHandler
        : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;


        public RegisterCommandHandler(
            IRepositoryWrapper repository,
            IBcryptHashing hashing)
        {
            _repository = repository;
            _hashing = hashing;
        }

        public async Task<Guid> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            // Check if organization already exists
            var existingOrganization = _repository.Organization
                .FindByConditionAsync(x =>
                    x.OrganizationName == request.OrganizationName)
                .FirstOrDefault();

            if (existingOrganization != null)
            {
                throw new ConflictCustomException("Organization already exists.", "An organization with this name is already registered.");
            }

            // Get Email Verification record
            var emailVerification = _repository.EmailVerification
                .FindByConditionAsync(x =>
                    x.Email == request.Email &&
                    x.TemporaryVerificationToken == request.VerificationToken &&
                    x.IsVerified &&
                    x.IsActive)
                .FirstOrDefault();

            if (emailVerification == null)
            {
                throw new BadRequestCustomException("Invalid verification token.", "The verification token is invalid or the email has not been verified.");
            }

            // Check token expiry
            if (emailVerification.TemporaryVerificationTokenExpiresOn == null ||
                emailVerification.TemporaryVerificationTokenExpiresOn <= DateTime.UtcNow)
            {
                throw new BadRequestCustomException(
                    "Verification token has expired.",
                    "Please verify your email again.");
            }
           

            // Create Organization
            var organization = new Identity.Domain.Entities.Organization
            {
                Id = Guid.NewGuid(),
                OrganizationName = request.OrganizationName,
                OrganizationType = request.OrganizationType,
                Email = request.Email,
                Phone = request.Phone,
                Country = request.Country,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                PinCode = request.PinCode,
                EmailVerified = true
            };

            await _repository.Organization.CreateAsync(organization);
            // Create Person
            var person = new Person
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = request.PersonName,
                Email = request.PersonEmail,
                Phone = request.Phone,
                Country = request.Country,
                AddressLine = request.AddressLine1

            };

            await _repository.Person.CreateAsync(person);


            var user = new User
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                UserName = request.UserName,
                FailedLoginAttempts = 0,

            };

            user.UserSecret = _hashing.HashStringWithSalt(request.Password);

            await _repository.User.CreateAsync(user);
            string roleName = request.OrganizationType switch
            {
                OrganizationType.Supplier => Common.SUPPLIER_NETWORK_ADMIN_KEY,
                OrganizationType.Buyer => Common.BUYER_NETWORK_ADMIN_KEY,
                OrganizationType.Platform => Common.PLATFORM_ADMINISTRATOR,
                _ => throw new BadRequestCustomException("Invalid organization type.", "The provided organization type is not supported.")
            };
            var role = _repository.Role
                .FindByConditionAsync(x => x.UserRole == roleName)
                .FirstOrDefault();
            var userRoleMapping = new UserRoleMapping
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id

            };

            await _repository.UserRoleMapping.CreateAsync(userRoleMapping);



            _repository.EmailVerification.Update(emailVerification);

            await _repository.SaveAsync();

            return organization.Id;
        }
    }
}