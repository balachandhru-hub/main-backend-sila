using Buyer.Application.Features.Commands.AddPersonalWishlistItems;
using Buyer.Application.Features.Commands.CreatePersonalWishlist;
using Buyer.Application.Features.Commands.DeletePersonalWishlist;
using Buyer.Application.Features.Commands.DeletePersonalWishlistItem;
using Buyer.Application.Features.Commands.UpdatePersonalWishlist;
using Buyer.Application.Features.Commands.UpdatePersonalWishlistItem;
using Buyer.Application.Features.Queries.GetPersonalWishlist;
using Buyer.Application.Features.Queries.GetPersonalWishlists;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Personal wishlists: named lists of catalog products that only their owner sees.
    /// </summary>
    [ApiController]
    public class PersonalWishlistController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public PersonalWishlistController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/personal-wishlists")]
        [ApiAuthorization(Name = "GET_WISHLIST")]
        [SwaggerOperation("GetPersonalWishlists")]
        [SwaggerResponse(200, type: typeof(List<PersonalWishlistListItemDto>))]
        public async Task<IActionResult> List()
        {
            _logger.LogDebug("Fetching personal wishlists.");
            List<PersonalWishlistListItemDto> result = await _mediator.Send(new GetPersonalWishlistsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Personal wishlists fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/personal-wishlists")]
        [ApiAuthorization(Name = "CREATE_WISHLIST")]
        [SwaggerOperation("CreatePersonalWishlist")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Create([FromBody] PersonalWishlistWriteDto request)
        {
            _logger.LogDebug($"Creating personal wishlist. Name: {request.Name}");
            Guid id = await _mediator.Send(new CreatePersonalWishlistCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Personal wishlist created. WishlistId: {id}");
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Wishlist created."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/personal-wishlists/{wishlistId}")]
        [ApiAuthorization(Name = "GET_WISHLIST")]
        [SwaggerOperation("GetPersonalWishlist")]
        [SwaggerResponse(200, type: typeof(PersonalWishlistResponseDto))]
        public async Task<IActionResult> Get([FromRoute] Guid wishlistId)
        {
            _logger.LogDebug($"Fetching personal wishlist. WishlistId: {wishlistId}");
            PersonalWishlistResponseDto result = await _mediator.Send(new GetPersonalWishlistQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId
            });
            _logger.LogDebug($"Personal wishlist fetched. WishlistId: {wishlistId}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/personal-wishlists/{wishlistId}")]
        [ApiAuthorization(Name = "UPDATE_WISHLIST")]
        [SwaggerOperation("UpdatePersonalWishlist")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Update([FromRoute] Guid wishlistId, [FromBody] PersonalWishlistWriteDto request)
        {
            _logger.LogDebug($"Updating personal wishlist. WishlistId: {wishlistId}");
            await _mediator.Send(new UpdatePersonalWishlistCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId,
                Request = request
            });
            _logger.LogDebug($"Personal wishlist updated. WishlistId: {wishlistId}");
            return Ok(new SuccessResponseDto
            {
                Id = wishlistId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Wishlist updated."
            });
        }

        [HttpDelete]
        [Route("api/v1/buyer/personal-wishlists/{wishlistId}")]
        [ApiAuthorization(Name = "UPDATE_WISHLIST")]
        [SwaggerOperation("DeletePersonalWishlist")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Delete([FromRoute] Guid wishlistId)
        {
            _logger.LogDebug($"Deleting personal wishlist. WishlistId: {wishlistId}");
            await _mediator.Send(new DeletePersonalWishlistCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId
            });
            _logger.LogDebug($"Personal wishlist deleted. WishlistId: {wishlistId}");
            return Ok(new SuccessResponseDto
            {
                Id = wishlistId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Wishlist deleted."
            });
        }

        [HttpPost]
        [Route("api/v1/buyer/personal-wishlists/{wishlistId}/items")]
        [ApiAuthorization(Name = "UPDATE_WISHLIST")]
        [SwaggerOperation("AddPersonalWishlistItems")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> AddItems([FromRoute] Guid wishlistId, [FromBody] PersonalWishlistItemsWriteDto request)
        {
            _logger.LogDebug($"Adding products to personal wishlist. WishlistId: {wishlistId}, Products: {request.Items?.Count ?? 0}");
            await _mediator.Send(new AddPersonalWishlistItemsCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId,
                Request = request
            });
            _logger.LogDebug($"Products added to personal wishlist. WishlistId: {wishlistId}");
            return Ok(new SuccessResponseDto
            {
                Id = wishlistId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Products added to the wishlist."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/personal-wishlists/{wishlistId}/items/{itemId}")]
        [ApiAuthorization(Name = "UPDATE_WISHLIST")]
        [SwaggerOperation("UpdatePersonalWishlistItem")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> UpdateItem([FromRoute] Guid wishlistId, [FromRoute] Guid itemId, [FromBody] QuantityWriteDto request)
        {
            _logger.LogDebug($"Changing personal wishlist quantity. WishlistId: {wishlistId}, ItemId: {itemId}");
            await _mediator.Send(new UpdatePersonalWishlistItemCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId,
                ItemId = itemId,
                Request = request
            });
            _logger.LogDebug($"Personal wishlist quantity changed. WishlistId: {wishlistId}, ItemId: {itemId}");
            return Ok(new SuccessResponseDto
            {
                Id = itemId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Quantity updated."
            });
        }

        [HttpDelete]
        [Route("api/v1/buyer/personal-wishlists/{wishlistId}/items/{itemId}")]
        [ApiAuthorization(Name = "UPDATE_WISHLIST")]
        [SwaggerOperation("DeletePersonalWishlistItem")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> DeleteItem([FromRoute] Guid wishlistId, [FromRoute] Guid itemId)
        {
            _logger.LogDebug($"Removing product from personal wishlist. WishlistId: {wishlistId}, ItemId: {itemId}");
            await _mediator.Send(new DeletePersonalWishlistItemCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                WishlistId = wishlistId,
                ItemId = itemId
            });
            _logger.LogDebug($"Product removed from personal wishlist. WishlistId: {wishlistId}, ItemId: {itemId}");
            return Ok(new SuccessResponseDto
            {
                Id = itemId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Product removed from the wishlist."
            });
        }
    }
}
