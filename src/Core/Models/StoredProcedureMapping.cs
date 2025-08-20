namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Mapping information for a stored procedure invocation.
/// </summary>
public class StoredProcedureMapping
{
    public string MethodName { get; set; } = string.Empty;

    public string ReturnType { get; set; } = string.Empty;

    public List<ParameterMapping> Parameters { get; set; } = new List<ParameterMapping>();

    public string StoredProcName { get; set; } = string.Empty;
}
