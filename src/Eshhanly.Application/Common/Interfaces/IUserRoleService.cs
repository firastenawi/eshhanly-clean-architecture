namespace Eshhanly.Application.Common.Interfaces;

public interface IUserRoleService
{
    Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
