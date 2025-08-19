using System.Data.Linq.Mapping;

namespace LinqToSqlDemo;

// LINQ to SQL entity mapped via attributes
[Table(Name = "Customers")]
public class Customer
{
    [Column(IsPrimaryKey = true)]
    public int CustomerID { get; set; }

    [Column]
    public string? CompanyName { get; set; }
}
