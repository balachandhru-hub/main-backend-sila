using Buyer.Application.Contracts;
using Buyer.Application.Features.Commands.CreateMessage;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.CreateMessage
{
    public class GetMessageHistoryQueryHandler
        : IRequestHandler<GetMessageHistoryQuery, List<MessageResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIdentityApiClient _identityApiClient;

        public GetMessageHistoryQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIdentityApiClient identityApiClient)
        {
            _repository = repository;
            _logger = logger;
            _identityApiClient = identityApiClient;
        }

        public async Task<List<MessageResponseDto>> Handle(
            GetMessageHistoryQuery request,
            CancellationToken cancellationToken)
        {
            MessageThread? thread = await _repository.MessageThread
                .FindFirstByConditionAsync(x => x.Id == request.ThreadId && x.IsActive);

            if (thread == null)
            {
                throw new NotFoundCustomException("Conversation not found.", "Conversation does not exist.");
            }

            MessageParticipancy.ResolveForThread(
                _repository,
                thread,
                request.OrganizationId,
                request.OrganizationType);

            List<Domain.Entities.Message> messages = _repository.Message
                .FindByCondition(x => x.ThreadId == thread.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            Dictionary<Guid, string?> senderNames = new();

            try
            {
                List<Guid> senderIds = messages.Select(x => x.SenderUserId).Distinct().ToList();

                if (senderIds.Count > 0)
                {
                    List<IdentityUserDto> users = await _identityApiClient.GetUsersByIds(senderIds, cancellationToken);
                    senderNames = users.ToDictionary(x => x.UserId, x => x.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to resolve sender names for ThreadId: {thread.Id}. {ex.Message}");
            }

            List<MessageResponseDto> result = new();

            foreach (Domain.Entities.Message message in messages)
            {
                List<MessageAttachment> attachments = _repository.MessageAttachment
                    .FindByCondition(x => x.MessageId == message.Id && x.IsActive)
                    .ToList();

                result.Add(new MessageResponseDto
                {
                    Id = message.Id,
                    ThreadId = message.ThreadId,
                    SenderUserId = message.SenderUserId,
                    SenderName = senderNames.TryGetValue(message.SenderUserId, out string? name) ? name : null,
                    SenderOrganizationType = message.SenderOrganizationType,
                    Body = message.Body,
                    Attachments = attachments.Select(a => new MessageAttachmentResponseDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes
                    }).ToList(),
                    DateCreated = message.DateCreated,
                    IsReadByBuyer = message.IsReadByBuyer,
                    IsReadBySupplier = message.IsReadBySupplier
                });
            }

            result.Reverse();

            return result;
        }
    }
}
