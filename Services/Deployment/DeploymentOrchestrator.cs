using Blazor.Models.UI;
using Blazor.Models.Persistence;
using Blazor.Models.Podman;
using Blazor.Services.Persistence;

namespace Blazor.Services;

public class DeploymentOrchestrator
{
    private readonly PodmanExecutionService _podman;
    private readonly IDeploymentRepository _repo;

    public DeploymentOrchestrator(
        PodmanExecutionService podman,
        IDeploymentRepository repo)
    {
        _podman = podman;
        _repo = repo;
    }

    // Used by Dashboard "Test Podman"
    public async Task<bool> TestPodmanAsync(Action<string> log)
    {
        var result = await _podman.RunAsync(
            "version",
            log,
            log
        );

        return result.Success;
    }

    public async Task<bool> DeployAsync(
        DeploymentRequest request,
        Action<string> onStep,
        Action<string> onLog)
    {
        var deploymentId = Guid.NewGuid();

        // ---- ENV ----
        var registry = Environment.GetEnvironmentVariable("REGISTRY_URL");
        var user = Environment.GetEnvironmentVariable("REGISTRY_USER");
        var password = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD");

        if (string.IsNullOrWhiteSpace(registry))
            throw new Exception("REGISTRY_URL missing");

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new Exception("Registry credentials missing");

        // ---- CREATE DEPLOYMENT ----
        await _repo.CreateDeploymentAsync(new DeploymentEntity
        {
            Id = deploymentId,
            Application = request.SourceImage,
            Version = "latest",
            Station = "local",
            SourceImage = request.SourceImage,
            TargetImage = request.TargetImage,
            Status = "Running",
            StartedAt = DateTime.UtcNow
        });

        // ---- STEP RUNNER (IMPORTANT) ----
        async Task RunStepAsync(
            DeploymentStep step,
            Func<Task<PodmanCommandResult>> action)
        {
            // mark step started
            await _repo.UpsertStepAsync(new DeploymentStepEntity
            {
                DeploymentId = deploymentId,
                Step = step,
                Status = "Running",
                StartedAt = DateTime.UtcNow
            });

            var result = await action();

            // mark step finished
            await _repo.UpsertStepAsync(new DeploymentStepEntity
            {
                DeploymentId = deploymentId,
                Step = step,
                Status = result.Success ? "Success" : "Failed",
                FinishedAt = DateTime.UtcNow
            });

            if (!result.Success)
                throw new Exception($"{step} failed");
        }

        try
        {
            // -------- LOGIN --------
            onStep("Login");
            await RunStepAsync(
                DeploymentStep.Login,
                () => _podman.RunAsync(
                    $"login -u {user} --password-stdin {registry}",
                    onLog,
                    onLog,
                    stdin: password
                )
            );

            // -------- PULL --------
            onStep("Pull");
            await RunStepAsync(
                DeploymentStep.Pull,
                () => _podman.RunAsync(
                    $"pull {request.SourceImage}",
                    onLog,
                    onLog
                )
            );

            // -------- TAG --------
            onStep("Tag");
            await RunStepAsync(
                DeploymentStep.Tag,
                () => _podman.RunAsync(
                    $"tag {request.SourceImage} {request.TargetImage}",
                    onLog,
                    onLog
                )
            );

            // -------- PUSH --------
            onStep("Push");
            await RunStepAsync(
                DeploymentStep.Push,
                () => _podman.RunAsync(
                    $"push {request.TargetImage}",
                    onLog,
                    onLog
                )
            );

            // -------- DONE --------
            await _repo.MarkCompletedAsync(deploymentId, "Success");
            onStep("Deployment completed");

            return true;
        }
        catch (Exception ex)
        {
            await _repo.MarkCompletedAsync(deploymentId, "Failed");

            await _repo.AppendLogAsync(
                new DeploymentLogEntity(
                    deploymentId,
                    DateTimeOffset.UtcNow,
                    "ERROR",
                    ex.Message
                )
            );

            return false;
        }
    }
}
