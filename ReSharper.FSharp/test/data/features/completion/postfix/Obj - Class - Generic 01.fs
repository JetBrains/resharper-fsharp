// ${COMPLETE_ITEM:with}
module Module

[<AbstractClass>]
type Base<'T>() =
    abstract M: 'T -> unit

Base<int>().{caret}
