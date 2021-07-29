module Module

[<RequireQualifiedAccess>]
module Module1 =
    module Nested =
        type U =
            | A of int

module Module2 =
    let a{caret} = Module1.Nested.A 1
