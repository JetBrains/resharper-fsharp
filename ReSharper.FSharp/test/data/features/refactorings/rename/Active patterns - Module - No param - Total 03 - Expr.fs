//${NEW_NAME:Zzz}
module Module

let (|A|B|) = function _ -> A

match () with
| A -> ()

let a = (|A{caret}|B|)
