using System.Collections.Generic;

namespace System.Data.Linq;

public class DataContext
{
    protected DataContext(string connection)
    {
    }
}

public class Table<T> : List<T> { }
