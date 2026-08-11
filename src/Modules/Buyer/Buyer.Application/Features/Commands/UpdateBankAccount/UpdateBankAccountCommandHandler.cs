
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateBankAccount
{
    public class UpdateBankAccountCommandHandler
        : IRequestHandler<UpdateBankAccountCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateBankAccountCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateBankAccountCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Bank Account : {request.Id}");
            var data = request.Data;

            // 1. Check BuyerId
            if (data.BuyerId == Guid.Empty)
            {
                _logger.LogError(
                    "Invalid buyer information. BuyerId is required.");
                throw new PreConditionFailedCustomException(
                    "Invalid buyer information.",
                    "BuyerId is required."
                );
            }

           _logger.LogInfo(
                $"Fetching Bank Account for BuyerId: {data.BuyerId} and AccountId: {request.Id}");
            var bankAccount = await _repository.BuyerBankAccount
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.BuyerId == data.BuyerId)
                .FirstOrDefaultAsync(cancellationToken);

            _logger.LogInfo(
                bankAccount != null
                    ? $"Bank Account found for BuyerId: {data.BuyerId} and AccountId: {request.Id}"
                    : $"Bank Account NOT found for BuyerId: {data.BuyerId} and AccountId: {request.Id}");
            if (bankAccount == null)
            {
                _logger.LogError(
                    $"Bank Account NOT found for BuyerId: {data.BuyerId} and AccountId: {request.Id}");
                throw new NotFoundCustomException(
                    "Bank account not found.",
                    "The bank account does not exist or does not belong to the specified buyer."
                );
            }

            _logger.LogInfo(
                $"Saving changes for Bank Account Id: {request.Id}");

            if (data.AccountHolderName != null)
            {
                _logger.LogInfo(
                    $"Updating AccountHolderName for Bank Account Id: {request.Id}");
                bankAccount.AccountHolderName = data.AccountHolderName;
            }

            if (data.BankName != null)
            {
                _logger.LogInfo(
                    $"Updating BankName for Bank Account Id: {request.Id}");
                bankAccount.BankName = data.BankName;
            }

            if (data.BranchName != null)
            {
                _logger.LogInfo(
                    $"Updating BranchName for Bank Account Id: {request.Id}");
                bankAccount.BranchName = data.BranchName;
            }

            if (data.AccountNumber != null)
            {
                _logger.LogInfo(
                    $"Updating AccountNumber for Bank Account Id: {request.Id}");
                bankAccount.AccountNumber = data.AccountNumber;
            }

            if (data.IFSCCode != null)
            {
                _logger.LogInfo(
                    $"Updating IFSCCode for Bank Account Id: {request.Id}");
                bankAccount.IFSCCode = data.IFSCCode;
            }

            if (data.SWIFTCode != null)
            {
                _logger.LogInfo(
                    $"Updating SWIFTCode for Bank Account Id: {request.Id}");
                bankAccount.SWIFTCode = data.SWIFTCode;
            }

            if (data.Currency != null)
            {
                _logger.LogInfo(
                    $"Updating Currency for Bank Account Id: {request.Id}");
                bankAccount.Currency = data.Currency;
            }

            if (data.IsPrimary.HasValue)
            {
                _logger.LogInfo(
                    $"Updating IsPrimary for Bank Account Id: {request.Id}");
                bankAccount.IsPrimary = data.IsPrimary.Value;
            }

            if (data.IsVerified.HasValue)
            {
                _logger.LogInfo(
                    $"Updating IsVerified for Bank Account Id: {request.Id}");
                bankAccount.IsVerified = data.IsVerified.Value;
            }

        _repository.BuyerBankAccount.Update(bankAccount);
            _logger.LogInfo(
                $"Saving changes for Bank Account Id: {request.Id}");
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Changes saved successfully for Bank Account Id: {request.Id}");
            return bankAccount.Id;
        }
    }
}

