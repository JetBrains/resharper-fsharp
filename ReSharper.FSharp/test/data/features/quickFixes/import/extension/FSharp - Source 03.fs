module Top

module Nested =
    type List<'T> with
        member this.Prop = ()

[].Prop{caret}
