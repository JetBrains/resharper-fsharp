module Module

[<Interface>]
type I =
    abstract Foo: I

let (i1: I) = Unchecked.defaultof<I{caret}>
let (i2: I) = obj() :?> I
