let (|A|) (l: _ list) x = x :: l

match 1 with
| A [{caret}] _ -> ()
