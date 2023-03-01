module Say

match true, true{caret} with
| true, true -> failwith "todo"
| _, false -> failwith "todo"
