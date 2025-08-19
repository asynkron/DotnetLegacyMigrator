namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Represents an entity discovered in legacy metadata.
/// </summary>
public class Entity
{
    public string Name { get; set; }
    public List<EntityProperty> Properties { get; set; }
    public string TableName { get; set; }
}
