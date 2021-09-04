module Module

let (|A|) x = x + 1

match 1 with
| A{on} x -> ()
