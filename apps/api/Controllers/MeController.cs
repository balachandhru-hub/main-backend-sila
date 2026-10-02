using Microsoft.AspNetCore.Mvc;
using SilaMe.Api.Auth;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var session = HttpContext.Items[SessionContext.ItemKey] as Session;
        if (session is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        return Ok(userService.ToResponse(session.User, session.Application, session));
    }
}