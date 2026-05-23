using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }

    [HttpPost("microsoft-login")]
    public async Task<ActionResult<AuthResponseDto>> MicrosoftLogin([FromBody] string token)
    {
        if (string.IsNullOrEmpty(token)) return BadRequest("Token de Microsoft es requerido.");

        var result = await _authService.MicrosoftLoginAsync(token);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }
}
