using Contracts.IRepository;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Commands.UpdatePersonDetail
{
    public class UpdatePersonDetailCommandHandler
        : IRequestHandler<UpdatePersonDetailCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdatePersonDetailCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(
            UpdatePersonDetailCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating person details for PersonId: {request.PersonId}");

            var person = _repository.Person
                .FindFirstByCondition(x =>
                    x.Id == request.PersonId &&
                    x.IsActive);

            if (person == null)
            {
                _logger.LogError(
                    $"Person not found. PersonId: {request.PersonId}");

                throw new BadRequestCustomException(
                    "Person not found.",
                    $"Person with Id {request.PersonId} was not found.");
            }

            var data = request.Data;


            if (data.Name != null &&
                data.Name != person.Name)
            {
                _logger.LogInfo(
                    $"Updating Name for PersonId: {request.PersonId}");

                person.Name = data.Name;
            }


            if (data.Phone != null &&
                data.Phone != person.Phone)
            {
                _logger.LogInfo(
                    $"Updating Phone for PersonId: {request.PersonId}");

                person.Phone = data.Phone;
            }


            if (data.AddressLine != null &&
                data.AddressLine != person.AddressLine)
            {
                _logger.LogInfo(
                    $"Updating AddressLine for PersonId: {request.PersonId}");

                person.AddressLine = data.AddressLine;
            }


            if (data.Country != null &&
                data.Country != person.Country)
            {
                _logger.LogInfo(
                    $"Updating Country for PersonId: {request.PersonId}");

                person.Country = data.Country;
            }

            _repository.Person.Update(person);
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Person details updated successfully for PersonId: {request.PersonId}");

            return Unit.Value;
        }
    }
}