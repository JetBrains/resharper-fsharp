module Module

let (    |  B  |    C{caret} | ) x =
    if x then B else C

match true with
| B
| C -> ()
