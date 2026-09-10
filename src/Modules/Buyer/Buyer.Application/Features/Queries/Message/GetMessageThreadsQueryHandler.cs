using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.Message
{
    public class GetMessageThreadsQueryHandler
        : IRequestHandler<GetMessageThreadsQuery, List<MessageThreadSummaryDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public GetMessageThreadsQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<List<MessageThreadSummaryDto>> Handle(
            GetMessageThreadsQuery request,
            CancellationToken cancellationToken)
        {
            RFQ? rfq = await _repository.RFQ
                .FindFirstByConditionAsync(x => x.Id == request.RFQId && x.IsActive);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. RFQId: {request.RFQId}");
                throw new NotFoundCustomException("RFQ not found.", "RFQ does not exist.");
            }

            bool isBuyer = string.Equals(request.OrganizationType, "Buyer", StringComparison.OrdinalIgnoreCase);
            bool isSupplier = string.Equals(request.OrganizationType, "Supplier", StringComparison.OrdinalIgnoreCase);

            List<MessageThread> threads;

            if (isBuyer)
            {
                BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile
                    .FindFirstByCondition(x => x.OrganizationId == request.OrganizationId && x.IsActive);

                if (buyer == null || buyer.Id != rfq.BuyerId)
                {
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
                }

                threads = _repository.MessageThread
                    .FindByCondition(x => x.RFQId == request.RFQId && x.IsActive)
                    .ToList();
            }
            else if (isSupplier)
            {
                RFQOrganizationUserMapping? orgMapping = _repository.RFQOrganizationUserMapping
                    .FindFirstByCondition(x => x.RFQId == request.RFQId && x.OrganizationId == request.OrganizationId && x.IsActive);

                if (orgMapping == null)
                {
                    throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
                }

                threads = _repository.MessageThread
                    .FindByCondition(x => x.RFQId == request.RFQId && x.SupplierId == orgMapping.SupplierId && x.IsActive)
                    .ToList();
            }
            else
            {
                throw new ForBiddenCustomException("Forbidden", "Unknown organization type.");
            }

            if (threads.Count == 0)
            {
                _logger.LogError($"No message threads found. RFQId: {request.RFQId}");
                throw new NotFoundCustomException("No conversations found.", "No message threads exist for this RFQ.");
            }

            List<MessageThreadSummaryDto> result = new();

            foreach (MessageThread thread in threads)
            {
                int unreadCount = isBuyer
                    ? _repository.Message.FindByCondition(x => x.ThreadId == thread.Id && x.IsActive && !x.IsReadByBuyer).Count()
                    : _repository.Message.FindByCondition(x => x.ThreadId == thread.Id && x.IsActive && !x.IsReadBySupplier).Count();

                Domain.Entities.Message? lastMessage = _repository.Message
                    .FindByCondition(x => x.ThreadId == thread.Id && x.IsActive)
                    .OrderByDescending(x => x.DateCreated)
                    .FirstOrDefault();

                string? counterpartyName = null;

                try
                {
                    if (isBuyer)
                    {
                        SupplierProfileDto supplier = await _supplierApiClient.GetSupplierById(thread.SupplierId, cancellationToken);
                        counterpartyName = supplier?.BusinessProfile?.OrganizationName;
                    }
                    else
                    {
                        BuyerBusinessProfile? buyerProfile = _repository.BuyerBusinessProfile
                            .FindFirstByCondition(x => x.Id == thread.BuyerId && x.IsActive);
                        counterpartyName = buyerProfile?.OrganizationName;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unable to resolve counterparty name for ThreadId: {thread.Id}. {ex.Message}");
                }

                result.Add(new MessageThreadSummaryDto
                {
                    ThreadId = thread.Id,
                    RFQId = thread.RFQId,
                    RFQNumber = thread.RFQNumber,
                    BuyerId = thread.BuyerId,
                    SupplierId = thread.SupplierId,
                    CounterpartyName = counterpartyName,
                    LastMessageBody = lastMessage?.Body,
                    LastMessageAt = thread.LastMessageAt,
                    UnreadCount = unreadCount
                });
            }

            return result.OrderByDescending(x => x.LastMessageAt).ToList();
        }
    }
}
