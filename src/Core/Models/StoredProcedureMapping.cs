namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Mapping information for a stored procedure invocation.
/// </summary>
public class StoredProcedureMapping
{
    public string MethodName { get; set; }
    public string ReturnType { get; set; }
    public List<ParameterMapping> Parameters { get; set; } = new();
    public string StoredProcName { get; set; }
}
