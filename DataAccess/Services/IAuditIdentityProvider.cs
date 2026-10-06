namespace DataAccess.Services;

public interface IAuditIdentityProvider
{
    string GetCurrentIdentity();
}
