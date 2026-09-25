// ${COMPLETE_ITEM:P}
module Module

[<AbstractClass>]
type Base() =
    abstract P: int
    default this.P = 1

let x =
    { new Base() with
        override this.{caret} }
