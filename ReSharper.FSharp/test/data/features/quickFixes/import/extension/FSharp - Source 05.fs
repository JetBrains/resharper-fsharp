module Top

module Nested =
    type List<'T> with
        member this.Method() = ()

[].Method{caret}()
