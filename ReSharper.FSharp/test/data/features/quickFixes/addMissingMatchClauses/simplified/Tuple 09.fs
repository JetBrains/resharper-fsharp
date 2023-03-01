module Say

match true, Some true{caret} with
| _, None -> failwith "todo"
| _, Some true -> failwith "todo"
| false, Some false -> failwith "todo"
