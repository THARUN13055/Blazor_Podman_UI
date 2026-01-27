using MudBlazor.Services;
using Blazor.Components;
using Blazor.Services;
using Blazor.Services.Persistence;
using Blazor.Services.Registry;

// 🔹 Load .env ONLY for local development
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();

// Podman backend services
builder.Services.AddSingleton<PodmanExecutionService>();
builder.Services.AddSingleton<IDeploymentRepository, DeploymentRepository>();
builder.Services.AddSingleton<DeploymentOrchestrator>();
builder.Services.AddSingleton<RegistrySearchService>();


var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
