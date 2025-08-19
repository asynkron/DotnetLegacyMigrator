using System;
using NHibernate;
using NHibernate.Cfg;

namespace NHibernateDemo;

// Minimal console that boots NHibernate using hibernate.cfg.xml
class Program
{
    static void Main()
    {
        var cfg = new Configuration();
        // configure from hibernate.cfg.xml
        cfg.Configure();
        // load mappings embedded in this assembly
        cfg.AddAssembly(typeof(Program).Assembly);

        using ISessionFactory sessionFactory = cfg.BuildSessionFactory();
        using ISession session = sessionFactory.OpenSession();

        Console.WriteLine("NHibernate session opened");
    }
}
