module Module

module Nested1 =
    type System.Int32 with
        member this.M() = ()

let i = 1
i.{caret}M()
