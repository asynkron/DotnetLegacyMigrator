namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Represents a navigation property between entities.
/// </summary>
public class Navigation
{
    public string Name { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public bool IsCollection { get; set; }
    public string? ForeignKey { get; set; }
    public string? AssociationName { get; set; }
    public string? JoinTable { get; set; }
    public string? Inverse { get; set; }
}
