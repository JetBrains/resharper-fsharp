//${NEW_NAME:Zzz}
module Module

let (    |  B  |    _ | ) x =
    if x then Some () else None

match true with
| B{caret}
| _ -> ()

let _ = (|B|_|)

let (|Id|) f x = x

match () with
| Id (|B|_|) 1 -> ()
