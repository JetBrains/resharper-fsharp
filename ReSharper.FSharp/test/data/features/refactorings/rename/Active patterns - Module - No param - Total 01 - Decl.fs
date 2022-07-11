//${NEW_NAME:Zzz}
module Module

let (|A{caret}|B|) = function _ -> A

match () with
| A -> ()

let a = (|A|B|)
