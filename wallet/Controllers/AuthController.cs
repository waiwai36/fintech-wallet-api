using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using wallet.Models.Requests;
using wallet.Models.Responses;
using wallet.Services;

namespace wallet.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        #region REGISTER
        /// <summary>
        /// Registers a new user and automatically creates their digital wallet.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserRegisterResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)] 
        public async Task<ActionResult<ApiResponse<UserRegisterResponse>>> Register([FromBody] RegisterRequest request)
        {
                    
            var result = await _authService.RegisterAsync(request);     
            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<UserRegisterResponse>.Ok(
                    result,
                    "User registered and digital wallet created successfully." 
                )
            );
        }

        #endregion

        #region LOGIN

        /// <summary>
        /// Authenticates a user.
        /// </summary>
        [HttpPost("login")]
        [EnableRateLimiting("StrictPolicy")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)] 
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return SuccessResponse(result, "Login successful.");
           
        }

        #endregion

        #region REFRESH TOKEN

        /// <summary>
        /// Generates a new Access Token and Refresh Token using a valid Refresh Token (Token Rotation).
        /// </summary>     
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)] 
        public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken(RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            return SuccessResponse(result, "Generate token successful");
        }   

        #endregion

        #region LOGOUT

        //[HttpPost("logout")]
        //public async Task<ActionResult<ApiResponse<object?>>> Logout(LogoutRequest request)
        //{
        //    await _authService.LogoutAsync(request.UserId);

        //    return Ok(ApiResponse<object?>.Ok(null, "Logged out successfully"));
        //}

        #endregion
    }
}
