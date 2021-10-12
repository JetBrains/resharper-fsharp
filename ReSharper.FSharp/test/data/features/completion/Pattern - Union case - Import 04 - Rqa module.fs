// ${COMPLETE_ITEM:B}
module Module

[<RequireQualifiedAccess>]
module Nested =
    type U =
        | A
        | B of int

match Nested.U.A with
| {caret}
