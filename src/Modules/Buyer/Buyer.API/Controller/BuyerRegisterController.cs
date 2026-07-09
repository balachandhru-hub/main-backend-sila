using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Buyer.Application.Features.Queries.GetOrganizationProfile;
using SharedKernel.LoggerServices;

using SharedKernel.Dto;
using SharedKernel.Attributes;



using Buyer.Domain.Dto;





namespace Buyer.API.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
       


        public AuthController(
            IMediator mediator,
            ILoggerManager logger,
            HttpClient httpClient,
            IConfiguration configuration
          
            )
        {
            _mediator = mediator;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
   
        }
        [HttpGet]
        [Route("api/v1/buyerregister/{organizationId}")]
        [ValidateModelState]
        [ApiAuthorization(Name ="GET_MY_PROFILE")]
        [SwaggerOperation("GetProfile")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "OTP generated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetOrganizationProfile(Guid organizationId)
{
    var result = await _mediator.Send(
        new GetOrganizationProfileQuery(organizationId));

    return Ok(result);
}
    }
}