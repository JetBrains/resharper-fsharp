module Say

[<RequireQualifiedAccess>]
module Nested =
    type E =
        | A = 1
        | B = 2

match Nested.E.A{caret} with
| Nested.E.A -> ()
