using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Supplier.API.Hubs;
using Supplier.Application.Contracts;
using Supplier.Domain.Dto;
using Swashbuckle.AspNetCore.Annotations;

namespace Supplier.API.Controllers
{
    /// <summary>
    /// Thin proxy for private Buyer&lt;-&gt;Supplier RFQ message threads. Message data is
    /// owned by the Buyer service - every action here just forwards the caller's
    /// authenticated request to it via IBuyerApiClient.
    /// </summary>
    [ApiController]
    public class MessageController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly IHubContext<NotificationHub> _hubContext;

        public MessageController(
            IMediator mediator,
            ILoggerManager logger,
            IBuyerApiClient buyerApiClient,
            IHubContext<NotificationHub> hubContext)
        {
            _mediator = mediator;
            _logger = logger;
            _buyerApiClient = buyerApiClient;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Route("api/v1/supplier/message")]
        [ValidateModelState]
        [ApiAuthorization(Name = "SEND_MESSAGE")]
        [SwaggerOperation("SendMessage")]
        [SwaggerResponse(200, type: typeof(MessageResponseDto), description: "Message sent successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto message)
        {
            _logger.LogDebug($"Sending message for RFQId: {message.RFQId}, SupplierId: {message.SupplierId}");

            MessageResponseDto result = await _buyerApiClient.SendMessage(message);

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/supplier/message/threads")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MESSAGE_THREADS")]
        [SwaggerOperation("GetMessageThreads")]
        [SwaggerResponse(200, type: typeof(List<MessageThreadSummaryDto>), description: "Success")]
        public async Task<IActionResult> GetMessageThreads([FromQuery] Guid rfqId)
        {
            var result = await _buyerApiClient.GetMessageThreads(rfqId);

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/supplier/message/thread/{threadId}/history")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MESSAGE_HISTORY")]
        [SwaggerOperation("GetMessageHistory")]
        [SwaggerResponse(200, type: typeof(List<MessageResponseDto>), description: "Success")]
        public async Task<IActionResult> GetMessageHistory(
            [FromRoute] Guid threadId,
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10)
        {
            var result = await _buyerApiClient.GetMessageHistory(threadId, index, limit);

            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/supplier/message/thread/{threadId}/read")]
        [ValidateModelState]
        [ApiAuthorization(Name = "MARK_MESSAGE_READ")]
        [SwaggerOperation("MarkThreadRead")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Conversation marked as read")]
        public async Task<IActionResult> MarkThreadRead([FromRoute] Guid threadId)
        {
            await _buyerApiClient.MarkThreadRead(threadId);

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Conversation marked as read."
            });
        }

        [HttpGet]
        [Route("api/v1/supplier/message/attachment/{attachmentId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "DOWNLOAD_MESSAGE_ATTACHMENT")]
        [SwaggerOperation("DownloadMessageAttachment")]
        [SwaggerResponse(200, type: typeof(MessageAttachmentFileDto), description: "Success")]
        public async Task<IActionResult> DownloadMessageAttachment([FromRoute] Guid attachmentId)
        {
            var result = await _buyerApiClient.DownloadMessageAttachment(attachmentId);

            return Ok(result);
        }

        /// <summary>
        /// Called server-to-server by the Buyer service right after it persists a new
        /// message, so this service can push it to its own connected Supplier clients.
        /// </summary>
        [HttpPost]
        [Route("api/v1/supplier/message/internal/notify")]
        [ValidateModelState]
        [ApiAuthorization(Name = "SEND_MESSAGE")]
        [SwaggerOperation("NotifyNewMessage")]
        [SwaggerResponse(200, description: "Notification relayed")]
        public async Task<IActionResult> NotifyNewMessage(
            [FromBody] MessageResponseDto message)
        {
            await _hubContext.Clients
                .Group(NotificationHub.GroupName(message.RFQId, message.SupplierId))
                .SendAsync("NewMessage", message);

            return Ok();
        }
    }
}
