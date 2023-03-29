module Say

match []{caret} with
| [_] -> ()
| true :: _ :: _ -> ()
| false :: _ :: _ -> ()
