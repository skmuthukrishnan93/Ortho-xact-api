

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ortho_xact_api.DTO;
using Ortho_xact_api.Services;

namespace Ortho_xact_api.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/syspro")]
    public class SysproController : ControllerBase
    {

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SysproLoginRequest request)
        {
            var syspro = new SysproWebService();
            var responseXml = await syspro.LoginAsync(request.Operator, request.Password, request.Company);
            return Ok(new { response = responseXml.Body.LogonResult });
        }
    }
}
