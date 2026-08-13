using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.CreateSupplierBankAccount
{
    public class CreateSupplierBankAccountCommandHandler : IRequestHandler<CreateSupplierBankAccountCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateSupplierBankAccountCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateSupplierBankAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating Bank Account for SupplierId: {request.SupplierId}");

            if (request.SupplierId == Guid.Empty)
            {
                _logger.LogError("SupplierId is empty. Cannot create bank account.");
                throw new PreConditionFailedCustomException("Invalid supplier information.", "SupplierId is required.");
            }

            _logger.LogInfo($"Checking for duplicate account number: {request.Data.AccountNumber} for SupplierId: {request.SupplierId}");

            var existing = _repository.SupplierBankAccount
                .FindByCondition(x =>
                    x.SupplierId == request.SupplierId &&
                    x.AccountNumber == request.Data.AccountNumber &&
                    x.IsActive)
                .Any();

            if (existing)
            {
                _logger.LogError($"Duplicate bank account. AccountNumber: {request.Data.AccountNumber} already exists for SupplierId: {request.SupplierId}");
                throw new ConflictCustomException("Duplicate bank account.", "A bank account with the same account number already exists for this supplier.");
            }

            _logger.LogInfo($"No duplicate found. Proceeding to create bank account for SupplierId: {request.SupplierId}");

            var bankAccount = new SupplierBankAccount
            {
                Id = Guid.NewGuid(),
                SupplierId = request.SupplierId,
                AccountHolderName = request.Data.AccountHolderName,
                BankName = request.Data.BankName,
                BranchName = request.Data.BranchName,
                AccountNumber = request.Data.AccountNumber,
                IFSCCode = request.Data.IFSCCode,
                SWIFTCode = request.Data.SWIFTCode,
                IBAN = request.Data.IBAN,
                Currency = request.Data.Currency,
                IsPrimary = request.Data.IsPrimary,
                IsVerified = false,
                IsActive = true
            };

            _repository.SupplierBankAccount.Create(bankAccount);
            _logger.LogInfo($"Saving bank account to database for SupplierId: {request.SupplierId}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Bank Account created successfully. Id: {bankAccount.Id}");
            return bankAccount.Id;
        }
    }
}
