namespace Misha.Application.Tenants;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsAdmin { get; }
}
