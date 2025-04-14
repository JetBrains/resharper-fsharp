module Top

module Nested =
    type System.Collections.IList with
        member this.Method() = ()

[|1|].Method{caret}()
