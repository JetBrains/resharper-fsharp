[<AbstractClass>]
type A() =
    abstract Item: int -> string with get, set

type {caret}T() =
    inherit A()
