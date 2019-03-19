module rec Module

let [<Literal>] ``Foo.bar`` = 123
match 123 with
| Module.``Foo.bar`` -> ()
| _ -> ()
