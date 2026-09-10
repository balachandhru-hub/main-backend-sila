using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.Message
{
    public class MarkThreadReadCommandHandler : IRequestHandler<MarkThreadReadCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public MarkThreadReadCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(MarkThreadReadCommand request, CancellationToken cancellationToken)
        {
            Domain.Entities.MessageThread? thread = await _repository.MessageThread
                .FindFirstByConditionAsync(x => x.Id == request.ThreadId && x.IsActive);

            if (thread == null)
            {
                throw new NotFoundCustomException("Conversation not found.", "Conversation does not exist.");
            }

            bool isBuyer = MessageParticipancy.ResolveForThread(
                _repository,
                thread,
                request.OrganizationId,
                request.OrganizationType);

            DateTime now = DateTime.UtcNow;

            List<Domain.Entities.Message> unreadMessages = isBuyer
                ? _repository.Message.FindByCondition(x => x.ThreadId == thread.Id && x.IsActive && !x.IsReadByBuyer).ToList()
                : _repository.Message.FindByCondition(x => x.ThreadId == thread.Id && x.IsActive && !x.IsReadBySupplier).ToList();

            if (unreadMessages.Count == 0)
            {
                return true;
            }

            foreach (Domain.Entities.Message message in unreadMessages)
            {
                if (isBuyer)
                {
                    message.IsReadByBuyer = true;
                    message.ReadByBuyerAt = now;
                }
                else
                {
                    message.IsReadBySupplier = true;
                    message.ReadBySupplierAt = now;
                }
            }

            _repository.Message.UpdateRange(unreadMessages);

            await _repository.SaveAsync();

            _logger.LogInfo($"Marked {unreadMessages.Count} messages as read. ThreadId: {thread.Id}");

            return true;
        }
    }
}
