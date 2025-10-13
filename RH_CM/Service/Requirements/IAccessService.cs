namespace RH_CM.Service.Requirements
{
    public interface IAccessService
    {
        Task<bool> HasAccessAsync(string userId, string controller, string action);
    }
}
