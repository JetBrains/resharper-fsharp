type R = { A: int; B: int }

let r (someOption : option int) : R = 
    ()
    match someOption with
    | Some value -> {A = value; B = value}
    | None -> {}{caret}