namespace Olli.Api.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "Olli.Api";
    public string Audience { get; set; } = "Olli.Client";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public string DemoUsername { get; set; } = "demo";
    public string DemoPassword { get; set; } = "olli123";
}
