// ${COMPLETE_ITEM:with}
module Module

[<AbstractClass>]
type Base() =
    abstract M: int -> unit
    abstract M: System.Text.StringBuilder -> unit

Base().{caret}
