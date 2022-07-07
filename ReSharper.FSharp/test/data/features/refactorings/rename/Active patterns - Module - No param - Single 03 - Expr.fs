//${NEW_NAME:Zzz}
module Module

let (|A|) = ()

match () with
| A -> ()

let a = (|A{caret}|)
