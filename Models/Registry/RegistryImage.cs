namespace Blazor.Models.Registry;

public class RegistryImage
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Official { get; set; }
    public bool Automated { get; set; }
}
