// ${COMPLETE_ITEM:new}
module Module

[<AbstractClass>]
type Base() =
    abstract M: int -> unit
    abstract M: System.Text.StringBuilder -> unit

Base().{caret}
