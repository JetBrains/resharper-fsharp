module Say

let (|Id|) x = x

match []{caret} with
| _ :: _ :: Id _ -> ()
