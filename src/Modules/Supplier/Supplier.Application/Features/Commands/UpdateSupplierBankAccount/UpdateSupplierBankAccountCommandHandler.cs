using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.UpdateSupplierBankAccount
{
    public class UpdateSupplierBankAccountCommandHandler
        : IRequestHandler<UpdateSupplierBankAccountCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateSupplierBankAccountCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateSupplierBankAccountCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Supplier Bank Account: {request.Id}");

            if (request.SupplierId == Guid.Empty)
            {
                _logger.LogError(
                    "Invalid supplier information. SupplierId is required.");

                throw new PreConditionFailedCustomException(
                    "Invalid supplier information.",
                    "SupplierId is required.");
            }

          

            _logger.LogInfo(
                $"Fetching Bank Account for SupplierId: {request.SupplierId} " +
                $"and AccountId: {request.Id}");

            var bankAccount = await _repository.SupplierBankAccount
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.SupplierId == request.SupplierId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (bankAccount == null)
            {
                _logger.LogError(
                    $"Bank Account NOT found for SupplierId: {request.SupplierId} " +
                    $"and AccountId: {request.Id}");

                throw new NotFoundCustomException(
                    "Bank account not found.",
                    "The bank account does not exist or does not belong to the specified supplier.");
            }

            var data = request.Data;

           

            if (data.AccountHolderName != null)
            {
                bankAccount.AccountHolderName = data.AccountHolderName;
            }

            if (data.BankName != null)
            {
                bankAccount.BankName = data.BankName;
            }

            if (data.BranchName != null)
            {
                bankAccount.BranchName = data.BranchName;
            }

            if (data.AccountNumber != null)
            {
                bankAccount.AccountNumber = data.AccountNumber;
            }

            if (data.IFSCCode != null)
            {
                bankAccount.IFSCCode = data.IFSCCode;
            }

            if (data.SWIFTCode != null)
            {
                bankAccount.SWIFTCode = data.SWIFTCode;
            }

            if (data.IBAN != null)
            {
                bankAccount.IBAN = data.IBAN;
            }

            if (data.Currency != null)
            {
                bankAccount.Currency = data.Currency;
            }

          

            if (data.IsPrimary.HasValue)
            {
                _logger.LogInfo(
                    $"Updating IsPrimary for Supplier Bank Account Id: {request.Id}");

                if (data.IsPrimary.Value)
                {
                    var existingPrimaryAccounts =
                        _repository.SupplierBankAccount
                            .FindByCondition(x =>
                                x.SupplierId == request.SupplierId &&
                                x.Id != request.Id &&
                                x.IsPrimary &&
                                x.IsActive)
                            .ToList();

                    foreach (var existingAccount in existingPrimaryAccounts)
                    {
                        existingAccount.IsPrimary = false;

                       

                        _logger.LogInfo(
                            $"Existing Primary Bank Account Id: {existingAccount.Id} " +
                            $"changed to IsPrimary = false.");
                    }
                      _repository.SupplierBankAccount.UpdateRange(existingPrimaryAccounts);
                }

                bankAccount.IsPrimary = data.IsPrimary.Value;
            }

           

            if (data.IsVerified.HasValue)
            {
                bankAccount.IsVerified = data.IsVerified.Value;
            }

           

            _repository.SupplierBankAccount.Update(bankAccount);

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier Bank Account updated successfully. " +
                $"Id: {bankAccount.Id}");

            return bankAccount.Id;
        }
    }
}