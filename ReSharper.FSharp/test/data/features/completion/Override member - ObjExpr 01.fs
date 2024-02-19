// ${COMPLETE_ITEM:override P}
module Module

[<AbstractClass>]
type Base() =
    abstract P: int
    default this.P = 1

{ new Base() with
    {caret} }
