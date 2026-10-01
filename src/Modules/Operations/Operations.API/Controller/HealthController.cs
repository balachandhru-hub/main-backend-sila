using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Operations.API.Controllers
{
    /// <summary>
    /// Liveness probe of the Operations service.
    /// </summary>
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [Route("api/v1/operations/healthz")]
        [SwaggerOperation("GetOperationsHealth")]
        public IActionResult Get()
        {
            return Ok(new { status = "ok" });
        }
    }
}
