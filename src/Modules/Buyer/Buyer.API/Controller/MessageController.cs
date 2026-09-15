using Buyer.API.Hubs;
using Buyer.Application.Features.Commands.CreateMessage;
using Buyer.Application.Features.Queries.CreateMessage;
using Buyer.Domain.Dto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Controller for private Buyer&lt;-&gt;Supplier RFQ message threads.
    /// </summary>
    [ApiController]
    public class MessageController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IHubContext<MessageHub> _hubContext;

        public MessageController(
            IMediator mediator,
            ILoggerManager logger,
            IHubContext<MessageHub> hubContext)
        {
            _mediator = mediator;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Route("api/v1/buyer/message")]
        [ValidateModelState]
        [ApiAuthorization(Name = "SEND_MESSAGE")]
        [SwaggerOperation("SendMessage")]
        [SwaggerResponse(200, type: typeof(MessageResponseDto), description: "Message sent successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto message)
        {
            Guid organizationId = GetOrganizationId();
            string organizationType = GetOrganizationType();
            Guid userId = GetUserId();

            _logger.LogDebug($"Sending message for RFQId: {message.RFQId}, SupplierId: {message.SupplierId}");

            MessageResponseDto result = await _mediator.Send(
                new SendMessageCommand(organizationId, organizationType, userId, message));

            try
            {
                IClientProxy group = _hubContext.Clients.Group(MessageHub.GroupName(result.RFQId, result.SupplierId));

                // "NewMessage" is for whoever has this exact thread open - append immediately.
                // "NewMessageNotification" reaches the same group (every user auto-joined to it
                // on connect) so someone logged in but viewing a different page can still raise
                // an unread badge instead of missing the message entirely.
                await group.SendAsync("NewMessage", result);
                await group.SendAsync("NewMessageNotification", result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to broadcast new message. ThreadId: {result.ThreadId}. {ex}");
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/message/threads")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MESSAGE_THREADS")]
        [SwaggerOperation("GetMessageThreads")]
        [SwaggerResponse(200, type: typeof(List<MessageThreadSummaryDto>), description: "Success")]
        public async Task<IActionResult> GetMessageThreads([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetMessageThreadsQuery
            {
                RFQId = rfqId,
                OrganizationId = GetOrganizationId(),
                OrganizationType = GetOrganizationType()
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/message/thread/{threadId}/history")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MESSAGE_HISTORY")]
        [SwaggerOperation("GetMessageHistory")]
        [SwaggerResponse(200, type: typeof(List<MessageResponseDto>), description: "Success")]
        public async Task<IActionResult> GetMessageHistory(
            [FromRoute] Guid threadId,
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10)
        {
            var result = await _mediator.Send(new GetMessageHistoryQuery
            {
                ThreadId = threadId,
                OrganizationId = GetOrganizationId(),
                OrganizationType = GetOrganizationType(),
                Index = index,
                Limit = limit
            });

            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/message/thread/{threadId}/read")]
        [ValidateModelState]
        [ApiAuthorization(Name = "MARK_MESSAGE_READ")]
        [SwaggerOperation("MarkThreadRead")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Conversation marked as read")]
        public async Task<IActionResult> MarkThreadRead([FromRoute] Guid threadId)
        {
            await _mediator.Send(new MarkThreadReadCommand(
                threadId,
                GetOrganizationId(),
                GetOrganizationType()));

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Conversation marked as read."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/message/attachment/{attachmentId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "DOWNLOAD_MESSAGE_ATTACHMENT")]
        [SwaggerOperation("DownloadMessageAttachment")]
        [SwaggerResponse(200, type: typeof(MessageAttachmentFileDto), description: "Success")]
        public async Task<IActionResult> DownloadMessageAttachment([FromRoute] Guid attachmentId)
        {
            var result = await _mediator.Send(new GetMessageAttachmentQuery
            {
                AttachmentId = attachmentId,
                OrganizationId = GetOrganizationId(),
                OrganizationType = GetOrganizationType()
            });

            return Ok(result);
        }
    }
}
