// Simplified strongly typed DataSet generated from NorthwindDataSet.xsd
using System.Data;

namespace TypedDataSetDemo
{
    public partial class NorthwindDataSet : DataSet
    {
        public NorthwindDataSet()
        {
            Tables.Add(new CustomersDataTable());
            Tables.Add(new OrdersDataTable());
        }

        public CustomersDataTable Customers => (CustomersDataTable)Tables["Customers"];
        public OrdersDataTable Orders => (OrdersDataTable)Tables["Orders"];

        public class CustomersDataTable : DataTable
        {
            public CustomersDataTable()
            {
                TableName = "Customers";
                Columns.Add("CustomerID", typeof(int));
                var company = Columns.Add("CompanyName", typeof(string));
                company.MaxLength = 100;
                var contact = Columns.Add("ContactName", typeof(string));
                contact.AllowDBNull = true;
                contact.MaxLength = 50;
            }

            public CustomersRow AddCustomersRow(int customerID, string companyName, string? contactName)
            {
                var row = (CustomersRow)NewRow();
                row.CustomerID = customerID;
                row.CompanyName = companyName;
                row.ContactName = contactName;
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
            public string? ContactName { get => (string?)this["ContactName"]; set => this["ContactName"] = value; }
        }

        public class OrdersDataTable : DataTable
        {
            public OrdersDataTable()
            {
                TableName = "Orders";
                var id = Columns.Add("OrderID", typeof(int));
                id.AllowDBNull = false;
                var cust = Columns.Add("CustomerID", typeof(int));
                cust.AllowDBNull = true;
                var desc = Columns.Add("Description", typeof(string));
                desc.AllowDBNull = false;
                desc.MaxLength = 50;
                var notes = Columns.Add("Notes", typeof(string));
                notes.AllowDBNull = true;
                notes.MaxLength = 255;
            }

            public OrdersRow AddOrdersRow(int orderID, int? customerID, string description, string? notes)
            {
                var row = (OrdersRow)NewRow();
                row.OrderID = orderID;
                row.CustomerID = customerID;
                row.Description = description;
                row.Notes = notes;
                Rows.Add(row);
                return row;
            }

            protected override DataRow NewRowFromBuilder(DataRowBuilder builder) => new OrdersRow(builder);
        }

        public class OrdersRow : DataRow
        {
            internal OrdersRow(DataRowBuilder builder) : base(builder) { }
            public int OrderID { get => (int)this["OrderID"]; set => this["OrderID"] = value; }
            public int? CustomerID { get => (int?)this["CustomerID"]; set => this["CustomerID"] = value; }
            public string Description { get => (string)this["Description"]; set => this["Description"] = value; }
            public string? Notes { get => (string?)this["Notes"]; set => this["Notes"] = value; }
        }
    }

    // Minimal TableAdapter demonstrating a stored procedure
    public partial class OrdersTableAdapter
    {
        private System.Data.SqlClient.SqlCommand[] _commandCollection = new System.Data.SqlClient.SqlCommand[1];
        private System.Data.SqlClient.SqlCommand[] CommandCollection => _commandCollection;

        public OrdersTableAdapter()
        {
            InitCommandCollection();
        }

        private void InitCommandCollection()
        {
            _commandCollection[0] = new System.Data.SqlClient.SqlCommand();
            _commandCollection[0].CommandText = "dbo.GetOrdersByCustomer";
        }

        [System.ComponentModel.DataObjectMethod(System.ComponentModel.DataObjectMethodType.Select, false)]
        public NorthwindDataSet.OrdersDataTable GetOrdersByCustomer(int customerId)
        {
            var command = this.CommandCollection[0];
            return new NorthwindDataSet.OrdersDataTable();
        }
    }
}
