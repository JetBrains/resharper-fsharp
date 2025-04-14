module Top

module Nested =
    type System.Array with
        member this.Prop = ()

[||].Prop{caret}
