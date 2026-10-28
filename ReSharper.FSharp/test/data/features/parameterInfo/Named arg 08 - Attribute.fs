type A(a: int, b: int, c: int) =
    inherit System.Attribute()

[<A(b = 1, a = 2{caret})>]
let x = 1
