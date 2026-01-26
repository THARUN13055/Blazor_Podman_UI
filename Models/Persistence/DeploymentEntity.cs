namespace Blazor.Models.Persistence;

public class DeploymentEntity
{
    public Guid Id { get; set; }
    public string Application { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Station { get; set; } = default!;
    public string SourceImage { get; set; } = default!;
    public string TargetImage { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
