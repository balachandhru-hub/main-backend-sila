using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateBankAccount
{
    public class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateBankAccountCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating Bank Account for BuyerId: {request.BuyerId}");

            if (request.BuyerId == Guid.Empty)
            {
                _logger.LogError("BuyerId is empty. Cannot create bank account.");
                throw new PreConditionFailedCustomException("Invalid buyer information.", "BuyerId is required.");
            }

            _logger.LogInfo($"Checking for duplicate account number: {request.Data.AccountNumber} for BuyerId: {request.BuyerId}");

            var existing = _repository.BuyerBankAccount
                .FindByCondition(x =>
                    x.BuyerId == request.BuyerId &&
                    x.AccountNumber == request.Data.AccountNumber &&
                    x.IsActive)
                .Any();

            if (existing)
            {
                _logger.LogError($"Duplicate bank account. AccountNumber: {request.Data.AccountNumber} already exists for BuyerId: {request.BuyerId}");
                throw new ConflictCustomException("Duplicate bank account.", "A bank account with the same account number already exists for this buyer.");
            }

            _logger.LogInfo($"No duplicate found. Proceeding to create bank account for BuyerId: {request.BuyerId}");



            if (request.Data.IsPrimary)
            {
                _logger.LogInfo(
                    $"New bank account is marked as Primary. Checking existing primary account for BuyerId: {request.BuyerId}");

                var existingPrimaryAccounts = _repository.BuyerBankAccount
                    .FindByCondition(x =>
                        x.BuyerId == request.BuyerId &&
                        x.IsPrimary &&
                        x.IsActive)
                    .ToList();

                foreach (var account in existingPrimaryAccounts)
                {
                    account.IsPrimary = false;

            

                    _logger.LogInfo(
                        $"Existing Primary Bank Account Id: {account.Id} " +
                        $"changed to IsPrimary = false.");
                }
                  _repository.BuyerBankAccount.UpdateRange(existingPrimaryAccounts);
            }
            var bankAccount = new BuyerBankAccount
            {
                Id = Guid.NewGuid(),
                BuyerId = request.BuyerId,
                AccountHolderName = request.Data.AccountHolderName,
                BankName = request.Data.BankName,
                BranchName = request.Data.BranchName,
                AccountNumber = request.Data.AccountNumber,
                IFSCCode = request.Data.IFSCCode,
                SWIFTCode = request.Data.SWIFTCode,
                Currency = request.Data.Currency,
                IsPrimary = request.Data.IsPrimary,
                IsVerified = false,
                IsActive = true
            };

            _repository.BuyerBankAccount.Create(bankAccount);
            _logger.LogInfo($"Saving bank account to database for BuyerId: {request.BuyerId}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Bank Account created successfully. Id: {bankAccount.Id}");
            return bankAccount.Id;
        }
    }
}
