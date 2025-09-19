module Module

type Type() =
    static let x = 4
    new (x: int) = Type()
    member _.M1 x = x + 1
    member private _.M2 x y = x(y)
    member _.M3 (x, ?y) = y.Value + 1
    member _.Prop1 = 5
    member val Prop2 = 5 with get, set
    member _.Prop3 with get () = 5 and set (x: int) = ()
