namespace Blazor.Models.Persistence;

public record DeploymentLogEntity(
    Guid DeploymentId,
    DateTimeOffset Timestamp,
    string Level,
    string Message
);
