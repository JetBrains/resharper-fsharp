module Module

[<RequireQualifiedAccess>]
module Module1 =
    type U =
        | A of int

module Module2 =
    let a{caret} = Module1.A 1
