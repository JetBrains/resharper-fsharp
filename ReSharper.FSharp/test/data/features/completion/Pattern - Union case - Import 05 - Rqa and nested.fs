// ${COMPLETE_ITEM:B}
module Module

[<RequireQualifiedAccess>]
module Rqa =
    module Nested =
        type U =
            | A
            | B of int

match Rqa.Nested.A with
| {caret}
