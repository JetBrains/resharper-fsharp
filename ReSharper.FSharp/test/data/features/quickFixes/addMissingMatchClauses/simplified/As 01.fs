module Say

match true, true{caret} with
| true, (true as x) -> ()
