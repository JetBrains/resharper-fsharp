module Say

match (true, true), true{caret} with
| (true, true), _ -> ()
