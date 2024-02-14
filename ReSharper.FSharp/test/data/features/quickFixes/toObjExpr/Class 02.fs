module Module

[<AbstractClass>]
type AbstractBaseClass(i: int) =
    abstract P: int

AbstractBaseClass(1){caret}
