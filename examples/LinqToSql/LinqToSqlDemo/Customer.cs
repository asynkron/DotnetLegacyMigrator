using System.Data.Linq.Mapping;

namespace LinqToSqlDemo;

// LINQ to SQL entity mapped via attributes with inheritance mappings
[Table(Name = "Customers")]
[InheritanceMapping(Code = "C", Type = typeof(Customer), IsDefault = true)]
[InheritanceMapping(Code = "P", Type = typeof(PreferredCustomer))]
public class Customer
{
    [Column(IsPrimaryKey = true)]
    public int CustomerID { get; set; }

    // Non-null string with explicit length
    [Column(DbType = "NVarChar(100)", CanBeNull = false)]
    public string CompanyName { get; set; } = string.Empty;

    // Nullable string with length
    [Column(DbType = "NVarChar(50)", CanBeNull = true)]
    public string? ContactName { get; set; }

    // Nullable primitive property
    [Column(CanBeNull = true)]
    public int? Age { get; set; }

    // Discriminator column used for inheritance
    [Column(IsDiscriminator = true, CanBeNull = false)]
    public string CustomerType { get; set; } = string.Empty;
}
