let f (s1: string) (s2: string) = ()
let g (s1: string, s2: string) = ()
let meaning = "42"

let sql1 = f "Select * from sqlite_master" "select * from sqlite_master"
let sql2 = g ("Select * from sqlite_master", "select * from sqlite_master")

let _ = $"select * from hello"
let _ = $"select * from table % {5} "
let _ = $"select * from table %d{5} "
let _ = $"select * from table %0d{5} "

let _ = @"select * from hello"
let _ = $@"select * from hello"
let _ = @$"select * from hello"

let _ = "select * from table" + " where something=2"
let _ = "select * from table where something=" + meaning + $" and meaning={meaning}"
let _ = "select * from table where something=" + 2.ToString() + " and meaning=meaning"
let _ = $"SELECT * FROM people where " + $"name = 2"
let _ = @"SELECT * FROM people where " + @"name = 2"
let _ = $@"SELECT * FROM people where " + $@"name = 2"
let _ = "select * from \"people\" where \"name\" = 'Alice'"
let _ = $"select * from \"people\" where \"name\" = 'Alice'"
let _ = @"select * from ""people"" where ""name"" = 'Alice'"
let _ = """select * from ""people"" where ""name"" = 'Alice'"""


let _ = "select"
     + " * "
      + "FROM"
      + " table"

let _ = @"select"
      + @" * "
      + @"FROM"
      + @" table"

let _= $"select"
     + $" * "
     + $"FROM"
     + $" table"

let _ = $@"select"
      + $@" * "
      + @$"FROM"
      + @$" table"

let _ = """select"""
      + $" * "
      + $"""FROM"""
      + @" table"

let _ = "select * \
from \
people where name = 'Alice'"

System.Console.WriteLine("select * from people where name = {0}", name)
System.Console.WriteLine("select * from people where name = {0} or name = {1}", name, name)

$""" select * from people where name = {{Name}} or name = {name} """
