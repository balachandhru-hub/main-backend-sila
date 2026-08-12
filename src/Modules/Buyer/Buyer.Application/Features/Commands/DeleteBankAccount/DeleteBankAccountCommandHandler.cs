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
            _logger.LogInfo($"Soft deleting Bank Account Id: {request.Id} for BuyerId: {request.BuyerId}");

            var bankAccount = await _repository.BuyerBankAccount
                .FindByCondition(x => x.Id == request.Id && x.BuyerId == request.BuyerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (bankAccount == null)
            {
                _logger.LogError($"Bank Account not found. Id: {request.Id}, BuyerId: {request.BuyerId}");
                throw new NotFoundCustomException("Bank account not found.", "The bank account does not exist or does not belong to the specified buyer.");
            }

            _logger.LogInfo($"Bank Account found. Marking as inactive. Id: {request.Id}");

            bankAccount.IsActive = false;
            _repository.BuyerBankAccount.Update(bankAccount);
            _logger.LogInfo($"Saving soft delete for Bank Account Id: {request.Id}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Bank Account soft deleted successfully. Id: {request.Id}");
            return request.Id;
        }
    }
}
