using System;

namespace TypedDataSetDemo
{
    // Demonstrates usage of a typed DataSet generated from an XSD.
    internal class Program
    {
        static void Main()
        {
            var ds = new NorthwindDataSet();
            ds.Customers.AddCustomersRow(1, "ABC Corp", null);
            ds.Orders.AddOrdersRow(1, 1, "First order", null);
            Console.WriteLine($"Customers count: {ds.Customers.Rows.Count}, Orders count: {ds.Orders.Rows.Count}");
        }
    }
}
