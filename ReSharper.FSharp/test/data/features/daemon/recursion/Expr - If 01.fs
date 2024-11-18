module Module

let rec f1 () =
    if true then
        f1 ()

let rec f2 () =
    if true then
        f2 ()

    ()



let rec f3 () =
    if true then
        f3 ()
    elif true then
        f3 ()


let rec f4 () =
    if true then
        f4 ()
    elif true then
        f4 ()

    ()


let rec f5 () =
    if true then
        f5 ()
    elif true then
        f5 ()
    elif true then
        f5 ()

let rec f6 () =
    if true then
        f6 ()
    elif true then
        f6 ()
    elif true then
        f6 ()

    ()
