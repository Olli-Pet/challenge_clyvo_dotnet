using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Olli.Api.Controllers;
using Olli.Api.Infrastructure.Security;

namespace Olli.Api.Tests.Unit;

public sealed class AuthControllerTests
{
    private static AuthController CreateController() => new(Options.Create(new JwtOptions
    {
        Key = "unit-test-key-with-at-least-32-characters",
        DemoUsername = "demo",
        DemoPassword = "olli123"
    }));

    [Fact]
    [Trait("Category", "Unit")]
    public void Token_ComCredenciaisValidas_DeveEmitirBearer()
    {
        var result = CreateController().Token(new TokenRequest("demo", "olli123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var token = Assert.IsType<TokenResponse>(ok.Value);
        Assert.Equal("Bearer", token.TokenType);
        Assert.NotEmpty(token.AccessToken);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Token_ComCredenciaisInvalidas_DeveRetornar401()
    {
        var result = CreateController().Token(new TokenRequest("demo", "senha-incorreta"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
