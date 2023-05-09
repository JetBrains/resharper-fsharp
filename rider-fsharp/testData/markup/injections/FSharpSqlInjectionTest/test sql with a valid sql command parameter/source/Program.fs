using System;
using System.Data.SqlClient;
namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var c = new SqlCommand("SELECT * FROM people WHERE age=@UserId");
            c.Parameters.Add(new SqlParameter("@UserId", "foo"));
        }
    }
}