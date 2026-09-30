using System.Text.Json;
using MediatR;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using HashingSystem;
using Buyer.Domain.Common;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.StoreQuotationAudit
{
    public class StoreQuotationAuditCommandHandler
        : IRequestHandler<StoreQuotationAuditCommand>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IAesEncryption _aesEncryption;
        private readonly ILoggerManager _logger;

        public StoreQuotationAuditCommandHandler(
            IRepositoryWrapper repository,
            IAesEncryption aesEncryption,
            ILoggerManager logger)
        {
            _repository = repository;
            _aesEncryption = aesEncryption;
            _logger = logger;
        }

        public async Task Handle(
            StoreQuotationAuditCommand command,
            CancellationToken cancellationToken)
        {
            var request = command.Request;

            // Convert audit request to JSON
            var json = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions
                {
                    WriteIndented = false
                });

            // Encrypt audit data
            var encryptedData = _aesEncryption.Encrypt(json);

            // Create blockchain audit record
            var blockchainRecord = new RFQBlockchainRecord
            {
                Id = Guid.NewGuid(),

                RFQId = request.BuyerRFQId,

                EntityType = Common.SUPPLIER_QUOTATION,

                EventType = Common.QUOTATION_SUBMITTED,

                DataHash = encryptedData,

                BlockchainTransactionId = null,

                BlockNumber = null,

                BlockchainNetwork = Common.HYPERLEDGER_FABRIC
            };

            _repository.RFQBlockchainRecord
                .Create(blockchainRecord);

            await _repository.SaveAsync();
        }
    }
}
