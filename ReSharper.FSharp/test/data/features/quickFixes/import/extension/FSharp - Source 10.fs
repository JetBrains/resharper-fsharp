module Top

module Nested =
    type System.Array with
        member this.Method() = ()

[|1|].Method{caret}()
