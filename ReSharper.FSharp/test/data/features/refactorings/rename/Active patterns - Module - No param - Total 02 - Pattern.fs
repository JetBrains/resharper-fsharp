//${NEW_NAME:Zzz}
module Module

let (|A|B|) = function _ -> A

match () with
| A{caret} -> ()

let a = (|A|B|)
