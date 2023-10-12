module Say

match Some(struct (1, 2)){caret} with
| Some(struct (_, x)) -> ()
