using Blazor.Models.Persistence;
using Dapper;
using Npgsql;

namespace Blazor.Services.Persistence;

public class DeploymentRepository : IDeploymentRepository
{
    private readonly string _connectionString;

    public DeploymentRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration["POSTGRES_CONN"]
            ?? throw new InvalidOperationException("POSTGRES_CONN is not configured");
    }

    public async Task<Guid> CreateDeploymentAsync(DeploymentEntity deployment)
    {
        const string sql = """
            INSERT INTO deployments
            (
                id,
                application,
                version,
                station,
                source_image,
                target_image,
                status,
                started_at,
                finished_at
            )
            VALUES
            (
                @Id,
                @Application,
                @Version,
                @Station,
                @SourceImage,
                @TargetImage,
                @Status,
                @StartedAt,
                @FinishedAt
            );
        """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, deployment);

        return deployment.Id;
    }

    public async Task AppendLogAsync(DeploymentLogEntity log)
    {
        const string sql = """
            INSERT INTO deployment_logs
            (
                deployment_id,
                timestamp,
                level,
                message
            )
            VALUES
            (
                @DeploymentId,
                @Timestamp,
                @Level,
                @Message
            );
        """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, log);
    }

    public async Task MarkCompletedAsync(Guid deploymentId, string status)
    {
        const string sql = """
            UPDATE deployments
            SET
                status = @Status,
                finished_at = now()
            WHERE id = @DeploymentId;
        """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            DeploymentId = deploymentId,
            Status = status
        });
    }
    public async Task<IEnumerable<DeploymentEntity>> GetDeploymentsAsync()
    {
        const string sql = """
            SELECT
                id,
                application,
                version,
                station,
                source_image AS SourceImage,
                target_image AS TargetImage,
                status,
                started_at AS StartedAt,
                finished_at AS FinishedAt
            FROM deployments
            ORDER BY started_at DESC;
        """;

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<DeploymentEntity>(sql);
    }

    public async Task<IEnumerable<DeploymentLogEntity>> GetLogsAsync(Guid deploymentId)
    {
        const string sql = """
            SELECT
                deployment_id AS DeploymentId,
                timestamp,
                level,
                message
            FROM deployment_logs
            WHERE deployment_id = @deploymentId
            ORDER BY timestamp;
        """;

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<DeploymentLogEntity>(
            sql,
            new { deploymentId }
        );
    }

    public async Task UpsertStepAsync(DeploymentStepEntity step)
    {
        const string sql = """
            INSERT INTO deployment_steps
                (deployment_id, step, status, started_at, finished_at)
            VALUES
                (@DeploymentId, @Step, @Status, @StartedAt, @FinishedAt)
            ON CONFLICT (deployment_id, step)
            DO UPDATE SET
                status = EXCLUDED.status,
                started_at = EXCLUDED.started_at,
                finished_at = EXCLUDED.finished_at;
        """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(sql, step);
    }

    public async Task<IEnumerable<DeploymentStepEntity>> GetStepsAsync(Guid deploymentId)
    {
        const string sql = """
            SELECT
                deployment_id AS DeploymentId,
                step,
                status,
                started_at AS StartedAt,
                finished_at AS FinishedAt
            FROM deployment_steps
            WHERE deployment_id = @deploymentId
            ORDER BY step;
        """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<DeploymentStepEntity>(sql, new { deploymentId });
    }
}
