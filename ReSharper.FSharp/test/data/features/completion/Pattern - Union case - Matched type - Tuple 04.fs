// ${COMPLETE_ITEM:U.B}
module Module

[<RequireQualifiedAccess>]
type U =
    | A
    | B of int

let t = U.A, 1

match t with
| {caret}
