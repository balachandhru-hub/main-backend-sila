using Buyer.Application.Features.Queries.ItemBuyerMaster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;
using Buyer.Application.Features.Commands.ItemBuyerMaster;
using Buyer.Domain.Dtos;
using SharedKernel.Controllers;
using Microsoft.AspNetCore.Http;
using Buyer.Application.Features.Commands.ItemBuyerMaster;

namespace Buyer.API.Controller
{
    [ApiController]
    public class ItemBuyerMasterController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ItemBuyerMasterController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get Item Buyer Master
        /// </summary>
        [HttpGet]
        [Route("api/v1/buyer/item-master")]
        [ValidateModelState]
        //[ApiAuthorization(Name = "GET_ITEM_BUYER_MASTER")]
        [SwaggerOperation("GetItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto))]
        public async Task<IActionResult> Get(
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10,
            [FromQuery] Guid? buyerId = null,
            [FromQuery] string? searchTerm = null)
        {
            _logger.LogInfo("Getting Item Buyer Master.");

            var result = await _mediator.Send(
                new GetItemBuyerMasterQuery
                {
                    Index = index,
                    Limit = limit,
                    BuyerId = buyerId,
                    SearchTerm = searchTerm
                });

            return Ok(result);
        }

        /// <summary>
        /// Create Item Buyer Master
        /// </summary>
        [HttpPost]
        [Route("api/v1/buyer/item-master")]
        [ValidateModelState]
        //[ApiAuthorization(Name = "CREATE_ITEM_BUYER_MASTER")]
        [SwaggerOperation("CreateItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto))]
        public async Task<IActionResult> Create(
            [FromBody] CreateItemBuyerMasterDto dto)
        {
            _logger.LogInfo("Creating Item Buyer Master.");

            var id = await _mediator.Send(
    new CreateItemBuyerMasterCommand(dto, GetOrganizationId()));

            _logger.LogInfo($"IsAuthenticated : {User.Identity?.IsAuthenticated}");

            foreach (var claim in User.Claims)
            {
                _logger.LogInfo($"{claim.Type} = {claim.Value}");
            }

            return Ok(id);
        }


        /// <summary>
        /// Upload Item Buyer Master Excel
        /// </summary>
        [HttpPost]
        [Route("api/v1/buyer/item-master/upload")]
        [ValidateModelState]
        //[ApiAuthorization(Name = "UPLOAD_ITEM_BUYER_MASTER")]
        [SwaggerOperation("UploadItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto))]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            _logger.LogInfo("Uploading Item Buyer Master Excel.");

            var recordsInserted = await _mediator.Send(
                new UploadItemBuyerMasterCommand(
                    file,
                    GetOrganizationId()));

            return Ok(new
            {
                Message = "Upload successful.",
                RecordsInserted = recordsInserted
            });
        }



        /// <summary>
        /// Update Item Buyer Master
        /// </summary>
        [HttpPut]
        [Route("api/v1/buyer/item-master")]
        [ValidateModelState]
        //[ApiAuthorization(Name = "UPDATE_ITEM_BUYER_MASTER")]
        [SwaggerOperation("UpdateItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto))]
        public async Task<IActionResult> Update(
            [FromBody] UpdateItemBuyerMasterDto dto)
        {
            _logger.LogInfo("Updating Item Buyer Master.");

            var result = await _mediator.Send(
                new UpdateItemBuyerMasterCommand(dto, GetOrganizationId()));

            return Ok(result);
        }

        /// <summary>
        /// Delete Item Buyer Master
        /// </summary>
        [HttpDelete]
        [Route("api/v1/buyer/item-master/{id}")]
        [ValidateModelState]
        //[ApiAuthorization(Name = "DELETE_ITEM_BUYER_MASTER")]
        [SwaggerOperation("DeleteItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto))]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInfo("Deleting Item Buyer Master.");

            var result = await _mediator.Send(
                new DeleteItemBuyerMasterCommand(id));

            return Ok(result);
        }
    }
}