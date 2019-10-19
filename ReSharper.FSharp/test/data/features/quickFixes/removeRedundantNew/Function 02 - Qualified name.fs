module Module

module A =
    type B = System.String

let B = 123 

{caret}new A.B(' ', 1) |> ignore
