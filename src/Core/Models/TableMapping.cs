namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Describes the link between a database table and an entity type.
/// </summary>
public class TableMapping
{
    public string Name { get; set; }
    public string EntityType { get; set; }
}
