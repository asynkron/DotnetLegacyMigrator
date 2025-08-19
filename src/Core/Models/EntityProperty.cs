namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Describes an individual property mapped from legacy data sources.
/// </summary>
public class EntityProperty
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Metadata { get; set; }
    public string ColumnName { get; set; }
    public string DbType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsDbGenerated { get; set; }
    public bool IsNullable { get; set; }
    public int Order { get; set; }
    public int? MaxLength { get; set; }
    public string ForeignKey { get; set; }
    public string ForeignEntity { get; set; }
    public bool ForeignMany { get; set; }
    public bool SelfMany { get; set; }
    public bool CascadeDelete { get; set; }
}
