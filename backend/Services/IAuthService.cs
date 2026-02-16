namespace UmiHealthPOS.Services
{
    public interface IAuthService
    {
        int GetCurrentTenantId();
        string GetCurrentUserId();
        bool IsAuthenticated();
    }
}
