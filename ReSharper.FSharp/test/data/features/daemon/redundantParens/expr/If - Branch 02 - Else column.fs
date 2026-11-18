module M

let a = true
let f () = ()

let v1 () =
    if a then
        (if a then f ())
        else f ()

let v2 () =
    if a then
        (if a then f ())
      else f ()
