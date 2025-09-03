module Top

module Nested =
    type List<'T> with
        member this.Method() = ()

[1].Method{caret}()
