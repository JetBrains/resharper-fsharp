module Test

type Sql1 = SqlCommandProvider<"select">
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
    jsx "console.log"
    tsx "console.log"
    f "123"

css $".body {123}"
css $@".body {123}"
css """.body {123}"""
css ($".body {123}")
