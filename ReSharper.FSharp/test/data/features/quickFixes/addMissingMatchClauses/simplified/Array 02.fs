module Say

match true, [|1|]{caret} with
| true, _ -> ()
