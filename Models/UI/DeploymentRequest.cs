namespace Blazor.Models.UI;

public class DeploymentRequest
{
    public string SourceImage { get; set; } = default!;
    public string TargetImage { get; set; } = default!;
}
