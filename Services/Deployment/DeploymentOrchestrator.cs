using Blazor.Models.UI;
using Blazor.Models.Persistence;
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

    // Test podman
    public async Task<bool> TestPodmanAsync(Action<string> log)
    {
        log("Running: podman version");

        var result = await _podman.RunAsync(
            "version",
            log,
            log
        );

        log(result.Success
            ? "Podman command succeeded"
            : "Podman command failed");

        return result.Success;
    }

    // Real deployment: pull → tag → push
    public async Task<bool> DeployAsync(
        DeploymentRequest request,
        Action<string> onStep,
        Action<string> onLog)
    {
        var deploymentId = Guid.NewGuid();

        await _repo.CreateDeploymentAsync(new DeploymentEntity
        {
            Id = deploymentId,
            Application = request.SourceImage,   // later you can parse app name
            Version = "latest",
            Station = "local",
            SourceImage = request.SourceImage,
            TargetImage = request.TargetImage,
            Status = "Running",
            StartedAt = DateTime.UtcNow,
            FinishedAt = null
        });


        // 🔥 QUICK PROOF (THIS IS WHAT YOU ASKED)
        await _repo.AppendLogAsync(new DeploymentLogEntity(
            deploymentId,
            DateTimeOffset.UtcNow,
            "INFO",
            "TEST: writing to postgres"
        ));

        onLog("TEST: writing to postgres");

        var registry = Environment.GetEnvironmentVariable("REGISTRY_URL");
        var user = Environment.GetEnvironmentVariable("REGISTRY_USER");
        var password = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD");

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            await _repo.MarkCompletedAsync(deploymentId, "Failed");
            return false;
        }

        // 🔹 Login
        onStep("Logging into registry");
        var login = await _podman.RunAsync(
            $"login -u {user} -p {password} {registry}",
            onLog,
            onLog
        );
        if (!login.Success)
        {
            await _repo.MarkCompletedAsync(deploymentId, "Failed");
            return false;
        }

        // 🔹 Pull
        onStep("Pulling image");
        var pull = await _podman.RunAsync(
            $"pull {request.SourceImage}",
            onLog,
            onLog
        );
        if (!pull.Success)
        {
            await _repo.MarkCompletedAsync(deploymentId, "Failed");
            return false;
        }

        // 🔹 Tag
        onStep("Tagging image");
        var tag = await _podman.RunAsync(
            $"tag {request.SourceImage} {request.TargetImage}",
            onLog,
            onLog
        );
        if (!tag.Success)
        {
            await _repo.MarkCompletedAsync(deploymentId, "Failed");
            return false;
        }

        // 🔹 Push
        onStep("Pushing image");
        var push = await _podman.RunAsync(
            $"push {request.TargetImage}",
            onLog,
            onLog
        );
        if (!push.Success)
        {
            await _repo.MarkCompletedAsync(deploymentId, "Failed");
            return false;
        }

        await _repo.MarkCompletedAsync(deploymentId, "Success");
        onStep("Deployment completed");

        return true;
    }
}
