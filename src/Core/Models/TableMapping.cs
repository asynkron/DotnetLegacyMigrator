namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Describes the link between a database table and an entity type.
/// </summary>
public class TableMapping
{
    public string Name { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? Schema { get; set; }

    public List<Navigation> Navigations { get; set; } = new List<Navigation>();
}
