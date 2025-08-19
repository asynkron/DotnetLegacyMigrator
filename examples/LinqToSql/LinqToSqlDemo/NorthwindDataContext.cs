using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace LinqToSqlDemo;

// Basic DataContext representing the legacy database
[Database(Name = "Northwind")]
public partial class NorthwindDataContext : DataContext
{
    public Table<Customer> Customers;

    public Table<Order> Orders;

    public NorthwindDataContext(string connection) : base(connection)
    {
    }

    // Example of a stored procedure mapped via FunctionAttribute
    [Function(Name = "dbo.GetOrderById")]
    public Order GetOrderById(int id)
    {
        return new Order();
    }
}
