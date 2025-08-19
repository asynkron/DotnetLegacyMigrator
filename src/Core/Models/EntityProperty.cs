namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Describes an individual property mapped from legacy data sources.
/// </summary>
public class EntityProperty
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string? DbType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsDbGenerated { get; set; }
    public bool IsNullable { get; set; }
}
