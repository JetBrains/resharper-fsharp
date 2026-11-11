module Top

module Nested =
    type System.String with
        member this.Prop = ()

let Prop = ""
Prop.Prop{caret}
