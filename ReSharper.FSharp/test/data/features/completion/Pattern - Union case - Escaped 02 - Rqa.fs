// ${COMPLETE_ITEM:U.B C}
module Module

[<RequireQualifiedAccess>]
type U =
    | A
    | ``B C`` of int

match U.A with
| {caret}
