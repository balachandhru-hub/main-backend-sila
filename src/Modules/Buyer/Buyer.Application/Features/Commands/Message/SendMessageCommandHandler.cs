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

            bool isExternalSupplierCaller = request.ExternalSupplierCallerId.HasValue;
            bool isBuyerCaller = !isExternalSupplierCaller
                && string.Equals(request.OrganizationType, Common.BUYER, StringComparison.OrdinalIgnoreCase);
            bool isBuyerSendingToExternalSupplier = isBuyerCaller && dto.ExternalSupplierId.HasValue;

            Guid buyerId;
            Guid? supplierId = null;
            Guid? externalSupplierId = null;
            bool isBuyerSender;

            if (isExternalSupplierCaller)
            {
                externalSupplierId = request.ExternalSupplierCallerId!.Value;
                buyerId = MessageParticipancy.ResolveForRFQAsExternalSupplier(_repository, rfq, externalSupplierId.Value, _logger);
                isBuyerSender = false;
            }
            else if (isBuyerSendingToExternalSupplier)
            {
                (buyerId, externalSupplierId) = MessageParticipancy.ResolveForRFQAsBuyer(
                    _repository, rfq, request.OrganizationId, dto.ExternalSupplierId!.Value, _logger);
                isBuyerSender = true;
            }
            else
            {
                (buyerId, Guid resolvedSupplierId, bool resolvedIsBuyerSender) = MessageParticipancy.ResolveForRFQ(
                    _repository,
                    rfq,
                    request.OrganizationId,
                    request.OrganizationType,
                    dto.SupplierId,
                    _logger);

                supplierId = resolvedSupplierId;
                isBuyerSender = resolvedIsBuyerSender;
            }

            bool isCounterpartySender = !isBuyerSender;
            Guid senderOrganizationId = isBuyerSender ? buyerId : (supplierId ?? externalSupplierId!.Value);

            // The group-conversation is keyed by (RFQId, SupplierId) or (RFQId, ExternalSupplierId),
            // never by UserId, so every user of that party - present or registered later - shares
            // the same thread.
            MessageThread? thread = supplierId.HasValue
                ? await _repository.MessageThread
                    .FindFirstByConditionAsync(x => x.RFQId == dto.RFQId && x.SupplierId == supplierId && x.IsActive)
                : await _repository.MessageThread
                    .FindFirstByConditionAsync(x => x.RFQId == dto.RFQId && x.ExternalSupplierId == externalSupplierId && x.IsActive);

            if (thread == null)
            {
                thread = new MessageThread
                {
                    Id = Guid.NewGuid(),
                    RFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    BuyerId = buyerId,
                    SupplierId = supplierId,
                    ExternalSupplierId = externalSupplierId,
                };

                _repository.MessageThread.Create(thread);

                // Save the thread first so Message.ThreadId
                // has a valid FK record in the database.
                await _repository.SaveAsync();
            }

            Guid messageId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            string senderOrganizationType = isBuyerSender
                ? Common.BUYER
                : (supplierId.HasValue ? Common.SUPPLIER : Common.EXTERNAL_SUPPLIER);

            Message message = new()
            {
                Id = messageId,
                ThreadId = thread.Id,
                SenderUserId = isExternalSupplierCaller ? null : request.UserId,
                SenderOrganizationType = senderOrganizationType,
                SenderOrganizationId = senderOrganizationId,
                Body = dto.Body,
                IsReadByBuyer = isBuyerSender,
                IsReadBySupplier = isCounterpartySender,
                ReadByBuyerAt = isBuyerSender ? now : null,
                ReadBySupplierAt = isCounterpartySender ? now : null
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

            string? senderName = null;

            if (isExternalSupplierCaller)
            {
                ExternalSupplier? externalSupplier = await _repository.ExternalSupplier
                    .FindFirstByConditionAsync(x => x.Id == externalSupplierId && x.IsActive);

                senderName = externalSupplier?.SupplierName;
            }
            else
            {
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
            }

            MessageResponseDto responseDto = new()
            {
                Id = messageId,
                ThreadId = thread.Id,
                RFQId = thread.RFQId,
                SupplierId = thread.SupplierId,
                ExternalSupplierId = thread.ExternalSupplierId,
                SenderUserId = message.SenderUserId,
                SenderName = senderName,
                SenderOrganizationType = message.SenderOrganizationType,
                Body = message.Body,
                Attachments = attachmentDtos,
                DateCreated = now,
                IsReadByBuyer = message.IsReadByBuyer,
                IsReadBySupplier = message.IsReadBySupplier
            };

            if (supplierId.HasValue)
            {
                // ExternalSupplier conversations have no separate microservice/hub to relay to -
                // the external supplier connects directly to this hub via its session token.
                try
                {
                    await _supplierApiClient.NotifyNewMessage(responseDto, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unable to relay new message notification to Supplier service. ThreadId: {thread.Id}. {ex.Message}");
                }
            }

            return responseDto;
        }
    }
}
