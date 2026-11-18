module M

let a = true

let v1 =
    if a then
        (if a then 1 else 2) else 3

let v2 =
    if a then
        (if a then 1 else 2)
    else
        3

let v3 =
    if a then
        (let x = 1
         x)
    else
        2

let v4 =
    if a then (if a then 1 else 2) else 3
