using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DataAccess.Security;

public static class ManagementServiceAuthenticationDefaults
{
    public const string Scheme = "FinanceSyncManagementService";
    public const string HeaderName = "X-InvEntry-Management-Key";
    public const string ConfigurationKey = "InvEntryManagement:ApiKey";
}

public sealed class ManagementServiceAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;
    public ManagementServiceAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
        : base(options, logger, encoder) => _configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = _configuration[ManagementServiceAuthenticationDefaults.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(expected))
            return Task.FromResult(AuthenticateResult.Fail("Management service authentication is not configured."));
        if (!Request.Headers.TryGetValue(ManagementServiceAuthenticationDefaults.HeaderName, out var supplied))
            return Task.FromResult(AuthenticateResult.NoResult());
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(supplied.ToString());
        if (a.Length != b.Length || !CryptographicOperations.FixedTimeEquals(a, b))
            return Task.FromResult(AuthenticateResult.Fail("Invalid management service credential."));
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "financetracker-service")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
