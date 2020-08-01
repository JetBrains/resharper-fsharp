type R = { A: int; B: int }

let r (shouldFail : bool) : R = 
    if shouldFail then
        failwith ""
    elif true then
        {}{caret}
    else
        failwith ""