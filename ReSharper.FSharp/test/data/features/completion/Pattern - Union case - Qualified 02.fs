// ${COMPLETE_ITEM:B}
module Module

[<RequireQualifiedAccess>]
type U =
    | A
    | B of int

match U.A with
| U.{caret}
| U.B i -> ()
