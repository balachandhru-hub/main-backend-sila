using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Identity;
using HashingSystem;

namespace Identity.Application.Features.Commands.RegisterOrganization
{
    public class RegisterOrganizationCommandHandler
        : IRequestHandler<RegisterOrganizationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;


        public RegisterOrganizationCommandHandler(
            IRepositoryWrapper repository,
            IBcryptHashing hashing)
        {
            _repository = repository;
            _hashing = hashing;
        }

        public async Task<Guid> Handle(
            RegisterOrganizationCommand request,
            CancellationToken cancellationToken)
        {
            // Check if organization already exists
            var existingOrganization = _repository.Organization
                .FindByConditionAsync(x =>
                    x.OrganizationName == request.OrganizationName)
                .FirstOrDefault();

            if (existingOrganization != null)
            {
                throw new Exception("Organization already exists.");
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
                throw new Exception("Invalid verification token.");
            }

            // Check token expiry
            if (emailVerification.TemporaryVerificationTokenExpiresOn == null ||
                emailVerification.TemporaryVerificationTokenExpiresOn <= DateTime.UtcNow)
            {
                throw new Exception(
                    "Verification token has expired. Please verify your email again.");
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
                Status = OrganizationStatus.Active,
                EmailVerified = true
            };

            await _repository.Organization.CreateAsync(organization);
             // Create Person
            var person = new Person
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = request.PersonName,
                Email = request.Email,
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
                LockoutEnd = null,
                LastFailedLogin = null
            };

            user.UserSecret = _hashing.HashStringWithSalt(request.Password);

            await _repository.User.CreateAsync(user);

            // Clear verification token after successful registration
            emailVerification.TemporaryVerificationToken = null;
            emailVerification.TemporaryVerificationTokenExpiresOn = null;

            _repository.EmailVerification.Update(emailVerification);

            await _repository.SaveAsync();

            return organization.Id;
        }
    }
}