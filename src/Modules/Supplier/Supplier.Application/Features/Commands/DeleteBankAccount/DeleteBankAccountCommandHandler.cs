using Supplier.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Commands.DeleteBankAccount
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
            _logger.LogInfo($"Soft deleting Bank Account Id: {request.Id} for SupplierId: {request.SupplierId}");

            var bankAccount = await _repository.SupplierBankAccount
                .FindByCondition(x => x.Id == request.Id && x.SupplierId == request.SupplierId)
                .FirstOrDefaultAsync(cancellationToken);

            if (bankAccount == null)
            {
                _logger.LogError($"Bank Account not found. Id: {request.Id}, SupplierId: {request.SupplierId}");
                throw new NotFoundCustomException("Bank account not found.", "The bank account does not exist or does not belong to the specified supplier.");
            }

            _logger.LogInfo($"Bank Account found. Marking as inactive. Id: {request.Id}");

           
            _repository.SupplierBankAccount.Delete(bankAccount);
            _logger.LogInfo($"Saving soft delete for Bank Account Id: {request.Id}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Bank Account soft deleted successfully. Id: {request.Id}");
            return request.Id;
        }
    }
}
