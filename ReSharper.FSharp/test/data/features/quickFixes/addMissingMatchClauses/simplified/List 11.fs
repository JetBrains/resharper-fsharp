module Say

let (|Id|) x = x

match []{caret} with
| _ :: Id _ -> ()
