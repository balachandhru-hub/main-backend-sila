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
                throw new PreConditionFailedCustomException("Invalid buyer information.", "BuyerId is required.");

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
            await _repository.SaveAsync();

            _logger.LogInfo($"Bank Account created successfully. Id: {bankAccount.Id}");
            return bankAccount.Id;
        }
    }
}
