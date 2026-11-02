using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Olli.Api.Infrastructure.Security;

namespace Olli.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController(IOptions<JwtOptions> options) : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    public ActionResult<TokenResponse> Token(TokenRequest request)
    {
        var settings = options.Value;
        if (!string.Equals(request.Username, settings.DemoUsername, StringComparison.Ordinal)
            || !string.Equals(request.Password, settings.DemoPassword, StringComparison.Ordinal))
        {
            return Unauthorized(new { message = "Usuário ou senha inválidos." });
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, "tutor")
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, expires: expires, signingCredentials: credentials);
        return Ok(new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expires));
    }
}

public sealed record TokenRequest(string Username, string Password);
public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string TokenType = "Bearer");
