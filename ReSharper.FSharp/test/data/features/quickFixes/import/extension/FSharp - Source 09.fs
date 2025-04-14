module Top

module Nested =
    type System.Array with
        member this.Method() = ()

[||].Method{caret}()
