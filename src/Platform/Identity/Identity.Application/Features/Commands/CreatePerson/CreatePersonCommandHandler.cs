using Contracts.IRepository;
using HashingSystem;
using Identity.Domain.Dto;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using System.Security.Claims;

namespace Identity.Application.Features.Commands.CreatePerson
{
    public class CreatePersonCommandHandler
        : IRequestHandler<CreatePersonCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;
        private readonly ILoggerManager _logger;
   

        public CreatePersonCommandHandler(
            IRepositoryWrapper repository,
            IBcryptHashing hashing,
            ILoggerManager logger
            )
        {
            _repository = repository;
            _hashing = hashing;
            _logger = logger;
           
        }

        public async Task<Guid> Handle(
            CreatePersonCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Creating a new person.");
            var model = request.Model;

            _logger.LogInfo($"Validating organization with ID: {request.OrganizationId}.");
            var organizationId = request.OrganizationId;

            _logger.LogInfo($"Checking if organization with ID: {organizationId} exists.");
            var existingUser = _repository.User
                .FindFirstByCondition(x => x.UserName == model.UserName);

            if (existingUser != null)
            {
                _logger.LogInfo("Username already exists.");
                throw new BadRequestCustomException(
                    "Username already exists.",
                    "Please choose a different username.");
            }
                
            _logger.LogInfo($"Checking if email {model.Email} already exists.");
            var existingPerson = _repository.Person
                .FindFirstByCondition(x => x.Email == model.Email);

            if (existingPerson != null)
            {
                _logger.LogInfo("Email already exists.");
                throw new BadRequestCustomException(
                    "Email already exists.",
                    "Please use a different email address.");
            }

            _logger.LogInfo($"Validating role with ID: {model.RoleId}.");
            var role = _repository.Role
                .FindFirstByCondition(x => x.Id == model.RoleId);

            if (role == null)
            {
                
            _logger.LogError("Invalid role ID provided.");
                throw new BadRequestCustomException(
                    "Invalid Role.",
                    "Please select a valid role.");
            }
            // Create Person
            var person = new Person
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Country = model.Country,
                AddressLine = model.AddressLine
            };

            await _repository.Person.CreateAsync(person);

            // Create User
            var user = new User
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                UserName = model.UserName,
                FailedLoginAttempts = 0
            };

            user.UserSecret = _hashing.HashStringWithSalt(model.Password);

            await _repository.User.CreateAsync(user);

            // User Role Mapping
            var mapping = new UserRoleMapping
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = model.RoleId
            };

            await _repository.UserRoleMapping.CreateAsync(mapping);

            await _repository.SaveAsync();
            _logger.LogInfo($"Person created successfully with ID: {person.Id}.");
            return person.Id;
        }
    }
}