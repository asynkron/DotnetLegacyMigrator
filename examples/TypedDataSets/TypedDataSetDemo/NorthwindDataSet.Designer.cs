// Simplified strongly typed DataSet generated from NorthwindDataSet.xsd
using System.Data;

namespace TypedDataSetDemo
{
    public partial class NorthwindDataSet : DataSet
    {
        public NorthwindDataSet()
        {
            Tables.Add(new CustomersDataTable());
        }

        public CustomersDataTable Customers => (CustomersDataTable)Tables["Customers"];

        public class CustomersDataTable : DataTable
        {
            public CustomersDataTable()
            {
                TableName = "Customers";
                Columns.Add("CustomerID", typeof(int));
                Columns.Add("CompanyName", typeof(string));
            }

            public CustomersRow AddCustomersRow(int customerID, string companyName)
            {
                var row = (CustomersRow)NewRow();
                row.CustomerID = customerID;
                row.CompanyName = companyName;
                Rows.Add(row);
                return row;
            }

            protected override DataRow NewRowFromBuilder(DataRowBuilder builder) => new CustomersRow(builder);
        }

        public class CustomersRow : DataRow
        {
            internal CustomersRow(DataRowBuilder builder) : base(builder) { }
            public int CustomerID { get => (int)this["CustomerID"]; set => this["CustomerID"] = value; }
            public string CompanyName { get => (string)this["CompanyName"]; set => this["CompanyName"] = value; }
        }
    }
}
