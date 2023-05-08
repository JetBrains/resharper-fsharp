module Test

type Sql = SqlCommandProvider<"1 + 1">
type Sql1 = SqlCommandProvider<"select * from table">
type Json = JsonProvider<"{ 1 }">
type Json1 = JsonProvider<"data.txt">
type Xml = XmlProvider<"<xml/>">
type Xml1 = XmlProvider<"data.txt">

let _ =
    css ".body {}"
    html "<head><title>1</title></head>"
    json "{ field: 1 }"
    js "console.log"
    javascript "console.log"
    f "123"

css $".body {123}"
css $@".body {123}"
css """.body {123}"""
css ($".body {123}")
