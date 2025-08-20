namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Represents a legacy data context and its associated mappings.
/// </summary>
public class DataContext
{
    public string Name { get; set; } = string.Empty;

    public List<TableMapping> Tables { get; set; } = new List<TableMapping>();

    public List<StoredProcedureMapping> StoredProcedures { get; set; } = new List<StoredProcedureMapping>();
}
