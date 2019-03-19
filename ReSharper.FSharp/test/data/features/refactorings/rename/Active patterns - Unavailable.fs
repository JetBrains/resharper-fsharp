module Module

let (    |  B {caret}|    C| ) x =
    if x then B else C

match true with
| B
| _ -> ()
