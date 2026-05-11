using AspNetCore_RealTimeSharedNotes.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore_RealTimeSharedNotes.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;

    public AuthApiController(IJwtTokenService jwtTokenService) => _jwtTokenService = jwtTokenService;

    //oauth2 post: /api/auth/token
    //body (application/x-www-form-urlencoded): grant_type, client_id, client_secret
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] string grant_type, [FromForm] string client_id, [FromForm] string client_secret)
    {
        if (grant_type != "client_credentials")
            return BadRequest(new { error = "unsupported_grant_type" });

        var token = await _jwtTokenService.CreateTokenAsync(client_id, client_secret);
        if (token == null)
            return Unauthorized(new { error = "invalid_client" });

        return Ok(new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = 3600
        });
    }
}
