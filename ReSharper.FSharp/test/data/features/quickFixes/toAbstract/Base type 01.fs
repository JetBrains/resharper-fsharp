namespace global

[<AbstractClass>]
type A() =
    abstract P: int

and {caret}B() =
    inherit A()
