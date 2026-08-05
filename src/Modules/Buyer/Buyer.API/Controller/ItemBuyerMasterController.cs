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
        [ApiAuthorization(Name = "GET_ITEM_BUYER_MASTER")]
        [SwaggerOperation("GetItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(ItemBuyerMasterDto), description: "Item Buyer Master retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> Get(
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10,
            [FromQuery] Guid? buyerId = null,
            [FromQuery] string? searchTerm = null)
        {
            _logger.LogDebug("Fetching Item Buyer Master.");

            var result = await _mediator.Send(
                new GetItemBuyerMasterQuery
                {
                    Index = index,
                    Limit = limit,
                    BuyerId = buyerId,
                    SearchTerm = searchTerm
                });

            _logger.LogDebug("Item Buyer Master retrieved successfully.");

            return Ok(result);
        }

        /// <summary>
        /// Create Item Buyer Master
        /// </summary>
        [HttpPost]
        [Route("api/v1/buyer/item-master")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_ITEM_BUYER_MASTER")]
        [SwaggerOperation("CreateItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Item Buyer Master created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> Create(
            [FromBody] CreateItemBuyerMasterDto dto)
        {
            _logger.LogDebug($"Creating Item Buyer Master. MaterialCode : {dto.MaterialCode}");

            var result = await _mediator.Send(
                new CreateItemBuyerMasterCommand(dto, GetOrganizationId()));

            _logger.LogDebug($"Item Buyer Master created successfully : {result}");

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Item Buyer Master created successfully",
                Description = "Item Buyer Master created successfully",
                StatusCode = 201
            });
        }


        /// <summary>
        /// Upload Item Buyer Master Excel
        /// </summary>
        [HttpPost]
        [Route("api/v1/buyer/item-master/upload")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPLOAD_ITEM_BUYER_MASTER")]
        [SwaggerOperation("UploadItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Item Buyer Master uploaded successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> Upload(IFormFile file, [FromQuery] Guid? buyerId)
        {
            _logger.LogDebug("Uploading Item Buyer Master Excel.");

            var dto = new UploadItemBuyerMasterDto
            {
                File = file,
                BuyerId = buyerId,
                OrganizationId = GetOrganizationId()
            };

            var result = await _mediator.Send(new UploadItemBuyerMasterCommand(dto));

            _logger.LogDebug($"Item Buyer Master uploaded successfully. Records : {result.SuccessfulUploads}");
            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = $"Uploaded {result.SuccessfulUploads} of {result.TotalRows} records.",
                Id = result.SuccessfulUploads.ToString()
            });
        }



        /// <summary>
        /// Update Item Buyer Master
        /// </summary>
        [HttpPut]
        [Route("api/v1/buyer/item-master/{id}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_ITEM_BUYER_MASTER")]
        [SwaggerOperation("UpdateItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Item Buyer Master updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Item Buyer Master not found")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateItemBuyerMasterDto dto)
        {
            _logger.LogDebug($"Updating Item Buyer Master : {id}");

            var result = await _mediator.Send(
                new UpdateItemBuyerMasterCommand(
                    id,
                    GetOrganizationId(),
                    dto));

            _logger.LogDebug($"Item Buyer Master updated successfully : {result}");

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Item Buyer Master updated successfully",
                Description = "Item Buyer Master updated successfully",
                StatusCode = 200
            });
        }

        /// <summary>
        /// Delete Item Buyer Master
        /// </summary>
        [HttpDelete]
        [Route("api/v1/buyer/item-master/{id}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "DELETE_ITEM_BUYER_MASTER")]
        [SwaggerOperation("DeleteItemBuyerMaster")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Item Buyer Master deleted successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Item Buyer Master not found")]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogDebug($"Deleting Item Buyer Master : {id}");

            await _mediator.Send(new DeleteItemBuyerMasterCommand(id));

            _logger.LogDebug($"Item Buyer Master deleted successfully : {id}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Item Buyer Master deleted successfully.",
                Id = id.ToString()
            });
        }
    }
}