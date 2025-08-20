using System.Data.Linq.Mapping;

namespace LinqToSqlDemo;

// Derived entity demonstrating inheritance mapping
public class PreferredCustomer : Customer
{
    // Additional column for the derived type
    [Column]
    public string? LoyaltyId { get; set; }
}
