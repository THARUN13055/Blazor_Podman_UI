using Blazor.Models.Persistence;

namespace Blazor.Services.Persistence;

public interface IDeploymentRepository
{
    Task<Guid> CreateDeploymentAsync(DeploymentEntity deployment);
    Task AppendLogAsync(DeploymentLogEntity log);
    Task MarkCompletedAsync(Guid deploymentId, string status);
    
    // adding the read method
    Task<IEnumerable<DeploymentEntity>> GetDeploymentsAsync();
    Task<IEnumerable<DeploymentLogEntity>> GetLogsAsync(Guid deploymentId);

}
