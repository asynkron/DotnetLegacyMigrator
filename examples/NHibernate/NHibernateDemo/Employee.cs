namespace NHibernateDemo;

// Derived entity mapped as an NHibernate subclass
public class Employee : Person
{
    public string Department { get; set; } = string.Empty;
}
