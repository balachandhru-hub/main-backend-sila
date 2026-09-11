using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateMessage
{
    public class SendMessageCommandHandler
        : IRequestHandler<SendMessageCommand, List<MessageResponseDto>>
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

        public async Task<List<MessageResponseDto>> Handle(
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

            bool isBuyerSender = string.Equals(request.OrganizationType, "Buyer", StringComparison.OrdinalIgnoreCase);

            // A Buyer can address several supplier group-conversations at once for the
            // same RFQ. A Supplier can only ever post into its own single group-conversation,
            // which MessageParticipancy resolves independently of the requested SupplierId.
            List<Guid?> requestedSupplierIds;

            if (isBuyerSender)
            {
                if (dto.SupplierId == null || dto.SupplierId.Count == 0)
                {
                    throw new BadRequestCustomException("Invalid request", "At least one SupplierId is required.");
                }

                requestedSupplierIds = dto.SupplierId.Distinct().Select(id => (Guid?)id).ToList();

                // Validate every target supplier up front so a request naming an uninvited
                // supplier fails atomically, instead of persisting the message for some
                // suppliers before failing partway through the list.
                foreach (Guid? requestedSupplierId in requestedSupplierIds)
                {
                    MessageParticipancy.ResolveForRFQ(
                        _repository,
                        rfq,
                        request.OrganizationId,
                        request.OrganizationType,
                        requestedSupplierId);
                }
            }
            else
            {
                requestedSupplierIds = new List<Guid?> { null };
            }

            string? senderName = null;

            try
            {
                List<IdentityUserDto> users = await _identityApiClient.GetUsersByIds(
                    new List<Guid> { request.UserId },
                    cancellationToken);

                senderName = users.FirstOrDefault()?.UserName;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Unable to resolve sender username for UserId: {request.UserId}. {ex.Message}");
            }

            List<MessageResponseDto> responses = new();

            foreach (Guid? requestedSupplierId in requestedSupplierIds)
            {
                MessageResponseDto response = await SendToSupplierGroup(
                    rfq,
                    requestedSupplierId,
                    dto,
                    request,
                    senderName,
                    cancellationToken);

                responses.Add(response);
            }

            return responses;
        }

        private async Task<MessageResponseDto> SendToSupplierGroup(
            RFQ rfq,
            Guid? requestedSupplierId,
            SendMessageDto dto,
            SendMessageCommand request,
            string? senderName,
            CancellationToken cancellationToken)
        {
            (Guid buyerId, Guid supplierId, bool isBuyerSender) = MessageParticipancy.ResolveForRFQ(
                _repository,
                rfq,
                request.OrganizationId,
                request.OrganizationType,
                requestedSupplierId);

            bool isSupplierSender = !isBuyerSender;
            Guid senderOrganizationId = isBuyerSender ? buyerId : supplierId;

            // The group-conversation is keyed by (RFQId, SupplierId), never by UserId, so every
            // user of that supplier - present or registered later - shares the same thread.
            MessageThread? thread = await _repository.MessageThread
                .FindFirstByConditionAsync(x => x.RFQId == rfq.Id && x.SupplierId == supplierId && x.IsActive);

            if (thread == null)
            {
                thread = new MessageThread
                {
                    Id = Guid.NewGuid(),
                    RFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    BuyerId = buyerId,
                    SupplierId = supplierId,
                };

                _repository.MessageThread.Create(thread);

                // Save the thread first so Message.ThreadId
                // has a valid FK record in the database.
                await _repository.SaveAsync();
            }

            Guid messageId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            Message message = new()
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

            bool hasAttachments = dto.Attachments != null && dto.Attachments.Count > 0;

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

            try
            {
                await _repository.SaveAsync();

                _logger.LogInfo(
                    $"Message saved. ThreadId: {thread.Id}, MessageId: {messageId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Failed to save message. ThreadId: {thread.Id}, MessageId: {messageId}. {ex.Message}");

                throw;
            }

            _logger.LogInfo($"Message sent successfully. ThreadId: {thread.Id}, MessageId: {messageId}");

            MessageResponseDto responseDto = new()
            {
                Id = messageId,
                ThreadId = thread.Id,
                RFQId = thread.RFQId,
                SupplierId = thread.SupplierId,
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
                await _supplierApiClient.NotifyNewMessage(responseDto, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to relay new message notification to Supplier service. ThreadId: {thread.Id}. {ex.Message}");
            }

            return responseDto;
        }
    }
}
