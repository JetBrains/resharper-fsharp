module Say

match []{caret} with
| [_] -> ()
| _ :: true :: _ -> ()
| _ :: false :: _ -> ()
