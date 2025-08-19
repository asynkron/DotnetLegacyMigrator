namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Represents the shape of data returned from a stored procedure.
/// </summary>
public class StoredProcedureResult
{
    public string Name { get; set; } = string.Empty;
    public List<EntityProperty> Properties { get; set; } = new();
}
