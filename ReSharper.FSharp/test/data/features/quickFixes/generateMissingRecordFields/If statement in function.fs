type R = { A: int; B: int }

let r (shouldFail : bool) : R = 
    ()
    if shouldFail then 
        failwith "" 
    else 
    if true then
        {}{caret}
    else
        failwith ""
