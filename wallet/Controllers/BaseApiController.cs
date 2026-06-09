using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using wallet.Models.Responses;

namespace wallet.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")] 
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected string CurrentUserName =>
             User.FindFirst("userName")?.Value
             ?? User.FindFirst(ClaimTypes.Name)?.Value
             ?? "system";
        protected int CurrentUserId
        {
            get
            {
                var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return int.TryParse(nameIdentifier, out int id) ? id : 0;
            }
        }
        protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message)
        {
            return Ok(ApiResponse<T>.Ok(data, message));
        }

        protected ActionResult<ApiResponse<object>> SuccessMessage(string message)
        {
            return Ok(ApiResponse<object>.Ok(null!, message));
        }
    }
}
