let (|A|_|) (l: _ list) x = Some(x :: l)

match 1 with
| A {caret}[] _ -> ()
