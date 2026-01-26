namespace Blazor.Models.Persistence;

public class DeploymentStepEntity
{
    public Guid DeploymentId { get; set; }
    public DeploymentStep Step { get; set; }
    public string Status { get; set; } = default!;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
