module Top

module Nested =
    type System.String with
        member this.Method() = ()

"".Method{caret}()
