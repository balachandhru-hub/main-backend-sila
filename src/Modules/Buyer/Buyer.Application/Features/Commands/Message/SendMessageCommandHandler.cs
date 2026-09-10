using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.Message
{
    public class SendMessageCommandHandler
        : IRequestHandler<SendMessageCommand, MessageResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IIdentityApiClient _identityApiClient;

        public SendMessageCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration,
            ISupplierApiClient supplierApiClient,
            IIdentityApiClient identityApiClient)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
            _supplierApiClient = supplierApiClient;
            _identityApiClient = identityApiClient;
        }

        public async Task<MessageResponseDto> Handle(
            SendMessageCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Message;

            bool hasBody = !string.IsNullOrWhiteSpace(dto.Body);
            bool hasAttachments = dto.Attachments != null && dto.Attachments.Count > 0;

            if (!hasBody && !hasAttachments)
            {
                throw new BadRequestCustomException(
                    "Invalid message",
                    "A message must contain text or at least one attachment.");
            }

            RFQ? rfq = await _repository.RFQ
                .FindFirstByConditionAsync(x => x.Id == dto.RFQId && x.IsActive);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. RFQId: {dto.RFQId}");
                throw new NotFoundCustomException("RFQ not found.", "RFQ does not exist.");
            }

            (Guid buyerId, Guid supplierId, bool isBuyerSender) = MessageParticipancy.ResolveForRFQ(
                _repository,
                rfq,
                request.OrganizationId,
                request.OrganizationType,
                dto.SupplierId);

            bool isSupplierSender = !isBuyerSender;
            Guid senderOrganizationId = isBuyerSender ? buyerId : supplierId;

            MessageThread? thread = await _repository.MessageThread
                .FindFirstByConditionAsync(x => x.RFQId == dto.RFQId && x.SupplierId == supplierId && x.IsActive);

            if (thread == null)
            {
                thread = new MessageThread
                {
                    Id = Guid.NewGuid(),
                    RFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    BuyerId = buyerId,
                    SupplierId = supplierId
                };

                _repository.MessageThread.Create(thread);
            }

            Guid messageId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            Domain.Entities.Message message = new()
            {
                Id = messageId,
                ThreadId = thread.Id,
                SenderUserId = request.UserId,
                SenderOrganizationType = isBuyerSender ? Common.BUYER : Common.SUPPLIER,
                SenderOrganizationId = senderOrganizationId,
                Body = dto.Body,
                IsReadByBuyer = isBuyerSender,
                IsReadBySupplier = isSupplierSender,
                ReadByBuyerAt = isBuyerSender ? now : null,
                ReadBySupplierAt = isSupplierSender ? now : null
            };

            _repository.Message.Create(message);

            List<MessageAttachmentResponseDto> attachmentDtos = new();

            if (hasAttachments)
            {
                string basePath = _configuration[Common.BASE_FOLDER_PATH]!;

                foreach (MessageAttachmentUploadDto attachment in dto.Attachments!)
                {
                    string folder = Path.Combine(
                        basePath,
                        Common.MESSAGE_ATTACHMENT_SUBFOLDER,
                        thread.Id.ToString(),
                        messageId.ToString());

                    Directory.CreateDirectory(folder);

                    string fullFilePath = Path.Combine(folder, attachment.FileName);

                    await File.WriteAllBytesAsync(fullFilePath, attachment.FileBytes, cancellationToken);

                    MessageAttachment attachmentEntity = new()
                    {
                        Id = Guid.NewGuid(),
                        MessageId = messageId,
                        FileName = attachment.FileName,
                        ContentType = attachment.ContentType,
                        FileSizeBytes = attachment.FileBytes.LongLength,
                        StoragePath = fullFilePath
                    };

                    _repository.MessageAttachment.Create(attachmentEntity);

                    attachmentDtos.Add(new MessageAttachmentResponseDto
                    {
                        Id = attachmentEntity.Id,
                        FileName = attachmentEntity.FileName,
                        ContentType = attachmentEntity.ContentType,
                        FileSizeBytes = attachmentEntity.FileSizeBytes
                    });
                }
            }

            thread.LastMessageAt = now;
            _repository.MessageThread.Update(thread);

            await _repository.SaveAsync();

            _logger.LogInfo($"Message sent successfully. ThreadId: {thread.Id}, MessageId: {messageId}");

            string? senderName = null;

            try
            {
                List<IdentityUserDto> users = await _identityApiClient.GetUsersByIds(new List<Guid> { request.UserId }, cancellationToken);
                senderName = users.FirstOrDefault()?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to resolve sender name for UserId: {request.UserId}. {ex.Message}");
            }

            MessageResponseDto responseDto = new()
            {
                Id = messageId,
                ThreadId = thread.Id,
                SenderUserId = request.UserId,
                SenderName = senderName,
                SenderOrganizationType = message.SenderOrganizationType,
                Body = message.Body,
                Attachments = attachmentDtos,
                DateCreated = now,
                IsReadByBuyer = message.IsReadByBuyer,
                IsReadBySupplier = message.IsReadBySupplier
            };

            try
            {
                await _supplierApiClient.NotifyNewMessage(thread.Id, responseDto, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to relay new message notification to Supplier service. ThreadId: {thread.Id}. {ex.Message}");
            }

            return responseDto;
        }
    }
}
