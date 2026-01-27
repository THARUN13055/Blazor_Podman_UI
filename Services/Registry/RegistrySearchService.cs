using System.Text.Json;
using Blazor.Models.Registry;

namespace Blazor.Services.Registry;

public class RegistrySearchService
{
    private readonly PodmanExecutionService _podman;

    public RegistrySearchService(PodmanExecutionService podman)
    {
        _podman = podman;
    }

    public async Task<List<RegistryImage>> SearchAsync(string query)
    {
        var outputLines = new List<string>();

        await _podman.RunAsync(
            $"search {query} --format json",
            line => outputLines.Add(line),
            _ => { }
        );

        var json = string.Join("", outputLines);

        if (string.IsNullOrWhiteSpace(json))
            return new List<RegistryImage>();

        var rawResults = JsonSerializer.Deserialize<List<PodmanSearchResult>>(json)
                         ?? new List<PodmanSearchResult>();

        return rawResults.Select(r => new RegistryImage
        {
            Name = r.Name ?? "",
            Description = r.Description ?? "",
            Official = r.Official == "[OK]",
            Automated = r.Automated == "[OK]"
        }).ToList();
    }

    // This matches Podman's ACTUAL JSON schema
    private class PodmanSearchResult
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        // IMPORTANT: these are STRINGS, not bools
        public string? Official { get; set; }
        public string? Automated { get; set; }
    }
}
