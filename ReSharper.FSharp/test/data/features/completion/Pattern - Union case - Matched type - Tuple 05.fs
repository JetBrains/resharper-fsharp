// ${COMPLETE_ITEM:U.B}
module Module

[<RequireQualifiedAccess>]
type U =
    | A
    | B of int

let t = 1, U.A

match t with
| _, {caret}
