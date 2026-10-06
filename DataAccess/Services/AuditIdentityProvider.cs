namespace DataAccess.Services;

public sealed class AuditIdentityProvider : IAuditIdentityProvider
{
    private const string DefaultIdentity = "INVENTRY";

    // Replace this implementation with the authenticated InvEntry/RBAC
    // operator identity when application-user authentication is available.
    public string GetCurrentIdentity()
    {
        return DefaultIdentity;
    }
}
