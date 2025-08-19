using System.Data.Linq.Mapping;

namespace LinqToSqlDemo;

// Additional entity to exercise more mapping scenarios
[Table(Name = "Orders")]
public class Order
{
    [Column(IsPrimaryKey = true)]
    public int OrderID { get; set; }

    // Non-null primitive
    [Column]
    public int CustomerID { get; set; }

    // Nullable string with length
    [Column(DbType = "NVarChar(200)", CanBeNull = true)]
    public string? Description { get; set; }

    // Nullable primitive
    [Column(CanBeNull = true)]
    public decimal? Amount { get; set; }

    // Non-null string with length
    [Column(DbType = "NVarChar(20)", CanBeNull = false)]
    public string Status { get; set; } = string.Empty;
}
