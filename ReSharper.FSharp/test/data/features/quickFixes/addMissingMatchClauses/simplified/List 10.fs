module Say

match []{caret} with
| [_] -> ()
| [_; _] -> ()
| _ :: true :: _ :: _ -> ()
| _ :: false :: _ :: _ -> ()
