module ControlFlow

let g i = i + 1

let getList () = [1; 2]

let run () =
    if g 1 > 1 then
        ()

    match g 1 with
    | 1 -> ()
    | _ -> ()

    for i in getList () do
        ()

    while g 0 > 1 do
        ()
