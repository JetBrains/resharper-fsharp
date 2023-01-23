module Say

match struct (true, true){caret} with
| struct (true, true) -> ()
| false, false -> ()
