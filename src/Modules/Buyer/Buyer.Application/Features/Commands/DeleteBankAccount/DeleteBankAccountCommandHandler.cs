using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeleteBankAccount
{
    public class DeleteBankAccountCommandHandler : IRequestHandler<DeleteBankAccountCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteBankAccountCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(DeleteBankAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting Bank Account Id: {request.Id}");

            var bankAccount = await _repository.BuyerBankAccount
                .FindByCondition(x => x.Id == request.Id && x.BuyerId == request.BuyerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (bankAccount == null)
                throw new NotFoundCustomException("Bank account not found.", "The bank account does not exist or does not belong to the specified buyer.");

            bankAccount.IsActive = false;
            _repository.BuyerBankAccount.Update(bankAccount);
            await _repository.SaveAsync();

            _logger.LogInfo($"Bank Account soft deleted successfully. Id: {request.Id}");
            return request.Id;
        }
    }
}
