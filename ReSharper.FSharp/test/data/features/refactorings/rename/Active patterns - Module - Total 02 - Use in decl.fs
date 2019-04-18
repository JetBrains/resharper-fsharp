//${NEW_NAME:Zzz}
module Module

let (    |  B  |    C | ) x =
    if x then B{caret} else C

match true with
| B
| C -> ()
