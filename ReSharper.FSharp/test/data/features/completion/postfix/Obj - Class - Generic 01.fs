// ${COMPLETE_ITEM:new}
module Module

[<AbstractClass>]
type Base<'T>() =
    abstract M: 'T -> unit

Base<int>().{caret}
