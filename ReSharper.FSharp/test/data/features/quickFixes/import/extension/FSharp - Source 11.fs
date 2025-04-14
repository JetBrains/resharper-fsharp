module Top

module Nested =
    type System.Collections.IList with
        member this.Prop = ()

[||].Prop{caret}
