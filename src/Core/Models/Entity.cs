namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Represents an entity discovered in legacy metadata.
/// </summary>
public class Entity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// If set, indicates the name of the base entity type this entity inherits from.
    /// </summary>
    public string? BaseType { get; set; }
    public List<EntityProperty> Properties { get; set; } = new();
    public string TableName { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public List<Navigation> Navigations { get; set; } = new();
}
