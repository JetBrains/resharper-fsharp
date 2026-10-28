type A(a: int, b: int, c: int) =
    inherit System.Attribute()

[<A(b = 1, a = {caret})>]
let x = 1
