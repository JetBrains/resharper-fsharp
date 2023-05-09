using System;
using System.Data.SqlClient;
using Dapper;

namespace ExecuteScalarEx
{
    class Program
    {
        static void Main(string[] args)
        {
            var cs = @"Server=localhost\\SQLEXPRESS;Database=testdb;Trusted_Connection=True;";
            using var con = new SqlConnection(cs);
            con.Open();
            var parameters = new { UserName = "Alice", Age=65535 };
            var sql = "select * from people where name = @UserName and age = @Age";
            var result = con.Query(sql, parameters);
            Console.WriteLine(result);
        }
    }
}