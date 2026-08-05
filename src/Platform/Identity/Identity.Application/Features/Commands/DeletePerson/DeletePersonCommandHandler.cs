using MediatR;
using Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Identity.Domain.Entities;

namespace Identity.Application.Features.Commands.DeletePerson
{
    public class DeletePersonCommandHandler
        : IRequestHandler<DeletePersonCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeletePersonCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            DeletePersonCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting Person : {request.PersonId}");

            var person = _repository.Person.FindFirstByCondition(x =>
                x.Id == request.PersonId &&
                x.IsActive);

            if (person == null)
            {
                _logger.LogError($"Person not found : {request.PersonId}");
                throw new NotFoundCustomException(
                    "Person not found.",
                    "Person not found.");
            }

            person.IsActive = false;

            _repository.Person.Update(person);
            _repository.Save();

            _logger.LogInfo($"Person deleted successfully : {request.PersonId}");

            return request.PersonId;
        }
    }
}