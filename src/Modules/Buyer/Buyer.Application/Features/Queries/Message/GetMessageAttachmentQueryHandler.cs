using Buyer.Application.Features.Commands.CreateMessage;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.CreateMessage
{
    public class GetMessageAttachmentQueryHandler
        : IRequestHandler<GetMessageAttachmentQuery, MessageAttachmentFileDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetMessageAttachmentQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<MessageAttachmentFileDto> Handle(
            GetMessageAttachmentQuery request,
            CancellationToken cancellationToken)
        {
            MessageAttachment? attachment = await _repository.MessageAttachment
                .FindFirstByConditionAsync(x => x.Id == request.AttachmentId && x.IsActive);

            if (attachment == null)
            {
                throw new NotFoundCustomException("Attachment not found.", "Attachment does not exist.");
            }

            Domain.Entities.Message? message = await _repository.Message
                .FindFirstByConditionAsync(x => x.Id == attachment.MessageId && x.IsActive);

            if (message == null)
            {
                throw new NotFoundCustomException("Attachment not found.", "Attachment does not exist.");
            }

            MessageThread? thread = await _repository.MessageThread
                .FindFirstByConditionAsync(x => x.Id == message.ThreadId && x.IsActive);

            if (thread == null)
            {
                throw new NotFoundCustomException("Attachment not found.", "Attachment does not exist.");
            }

            if (request.ExternalSupplierCallerId.HasValue)
            {
                MessageParticipancy.ResolveExternalThreadForExternalSupplier(thread, request.ExternalSupplierCallerId.Value, _logger);
            }
            else if (thread.ExternalSupplierId.HasValue)
            {
                MessageParticipancy.ResolveExternalThreadForBuyer(_repository, thread, request.OrganizationId, _logger);
            }
            else
            {
                MessageParticipancy.ResolveForThread(
                    _repository,
                    thread,
                    request.OrganizationId,
                    request.OrganizationType,
                    _logger);
            }

            if (!File.Exists(attachment.StoragePath))
            {
                throw new NotFoundCustomException("Attachment not found.", "Attachment file is missing.");
            }

            byte[] fileBytes = await File.ReadAllBytesAsync(attachment.StoragePath, cancellationToken);

            return new MessageAttachmentFileDto
            {
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileBytes = fileBytes
            };
        }
    }
}
