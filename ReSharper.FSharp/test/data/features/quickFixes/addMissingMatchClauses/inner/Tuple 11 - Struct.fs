module Say

match true, true{caret} with
| struct (false, false) -> ()
| true, true -> ()
