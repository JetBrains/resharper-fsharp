module Top

module Nested =
    type System.Array with
        member this.Prop = ()

[|1|].Prop{caret}
