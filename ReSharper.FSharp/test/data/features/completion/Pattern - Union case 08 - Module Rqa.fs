// ${COMPLETE_ITEM:Case}
module Module

[<RequireQualifiedAccess>]
module Nested =
    type U =
        | Case

match Nested.Case with
| Ca{caret}
