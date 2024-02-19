// ${COMPLETE_ITEM:override P}
module Module

[<AbstractClass>]
type Base() =
    abstract P: int

{ new Base() with
    {caret} }
