module Top

module Nested =
    type List<'T> with
        member this.Prop = ()

[1].Prop{caret}
