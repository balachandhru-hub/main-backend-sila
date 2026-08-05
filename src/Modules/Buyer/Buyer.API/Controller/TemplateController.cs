using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.Attributes;
using Buyer.Domain.Dto;
using SharedKernel.Controllers;
using Buyer.Application.Features.Commands.Template;
using Buyer.Application.Features.Queries.Template;
using Buyer.Application.Features.Queries.SupplierVerification;
using Buyer.Application.Features.Queries.SupplierVerificationRequest;
using Buyer.Application.Features.Commands.UpdateVerificationRequestStatus;

namespace Buyer.API.Controllers
{
    [ApiController]
    public class TemplateController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public TemplateController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpPost]
        [Route("api/v1/buyer/verification-template")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_VERIFICATION_TEMPLATE")]
        [SwaggerOperation("CreateVerificationTemplate")]
        [SwaggerResponse(200, type: typeof(Guid), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateVerificationTemplate(CreateVerificationTemplateDto dto)
        {
            Guid organizationId = GetOrganizationId();

            var id = await _mediator.Send(new CreateVerificationTemplateCommand
            {
                VerificationTemplateDto = dto,
                OrganizationId = organizationId
            });

            return Ok(id);
        }

        

       

        [HttpGet]
        [Route("api/v1/buyer/verification-template")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_VERIFICATION_TEMPLATES")]
        [SwaggerOperation("GetVerificationTemplates")]
        [SwaggerResponse(200, type: typeof(List<VerificationTemplateResponseDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetVerificationTemplates([FromQuery] Guid buyerId)
        {
            var result = await _mediator.Send(new GetVerificationTemplatesQuery
            {
                BuyerId = buyerId
            });

            return Ok(result);
        }
        [HttpPost]
        [Route("api/v1/buyer/verification-template-question")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_VERIFICATION_TEMPLATE_QUESTION")]
        [SwaggerOperation("CreateVerificationTemplateQuestion")]
        [SwaggerResponse(200, type: typeof(Guid), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateVerificationTemplateQuestion(
                [FromBody] CreateVerificationTemplateQuestionCommand command)
        {
            var questionId = await _mediator.Send(command);

            return Ok(new
            {
                Message = "Verification template question created successfully.",
                QuestionId = questionId
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/verification-template/{templateId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_VERIFICATION_TEMPLATE_BY_ID")]
        [SwaggerOperation("GetVerificationTemplateById")]
        [SwaggerResponse(200, type: typeof(VerificationTemplateResponseDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetVerificationTemplateById(Guid templateId)
        {
            var result = await _mediator.Send(new GetVerificationTemplateByIdQuery
            {
                TemplateId = templateId
            });

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/buyer/supplier-verification-request")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_VERIFICATION_REQUESTS")]
        [SwaggerOperation("GetSupplierVerificationRequests")]
        [SwaggerResponse(200, type: typeof(List<SupplierVerificationRequestListDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierVerificationRequests(
            Guid buyerId,
            int index,
            int limit)
        {
            var result = await _mediator.Send(
                new GetSupplierVerificationRequestListQuery
                {
                    BuyerOrganizationId = buyerId,
                    Index = index,
                    Limit = limit
                });

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/buyer/supplier-verification-request-detail")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_VERIFICATION_REQUEST_DETAIL")]
        [SwaggerOperation("GetSupplierVerificationRequestDetail")]
        [SwaggerResponse(200, type: typeof(SupplierVerificationRequestDetailDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierVerificationRequestDetail(Guid requestId)
        {
            var result = await _mediator.Send(
                new GetSupplierVerificationRequestDetailQuery
                {
                    RequestId = requestId
                });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/supplier-verification-request-by-supplier")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_VERIFICATION_REQUESTS_BY_SUPPLIER")]
        [SwaggerOperation("GetSupplierVerificationRequests")]
        [SwaggerResponse(200, type: typeof(List<SupplierVerificationRequestListDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierVerificationRequestsBySupplierId(
                   Guid supplierId,
                   int index,
                   int limit)
        {
            var result = await _mediator.Send(
                new GetSupplierVerificationRequestBySupplierIdQuery
                {
                    SupplierOrganizationId = supplierId,
                    Index = index,
                    Limit = limit
                });

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/buyer/supplier-verification-request-status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_VERIFICATION_REQUEST_STATUS")]
        [SwaggerOperation("UpdateSupplierVerificationRequestStatus")]
        [SwaggerResponse(200, type: typeof(bool), description: "Supplier verification request status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request ")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateVerificationRequestStatus(
    UpdateVerificationRequestStatusDto request)
        {
            
            var result = await _mediator.Send(
                new UpdateVerificationRequestStatusCommand(request));

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/answers")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_QUESTIONS_ANSWERS")]
        [SwaggerOperation("GetQuestionsAnswersForSupplier")]
        [SwaggerResponse(200, type: typeof(SupplierVerificationRequestDetailQuestinandAnswerDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetQuestionsAnswersForSupplier(Guid requestId)
        {
            Guid roleId = GetRoleId();
            var result = await _mediator.Send(
                new GetSupplierVerificationRequestDetailbyRequestIdQuery
                {
                    RequestId = requestId,
                    RoleId = roleId
                });

            return Ok(result);
        }
    }
}