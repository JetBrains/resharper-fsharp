module Module

open FSharp.Data.Sql

let [<Literal>] s = "hello"
let _ = new SqlDataProvider<s{caret}>()
