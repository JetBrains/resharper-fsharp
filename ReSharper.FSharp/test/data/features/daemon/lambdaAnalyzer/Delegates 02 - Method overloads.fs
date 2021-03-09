open System
open System.Runtime.CompilerServices

type A() =
    member x.M1(_: int, _: int) = ()
    member x.M1(_: int, a: Action<int>) = ()
    member x.M2(_: int, _: int) = ()

[<Extension>]
type Extensions() =
    [<Extension>]
    static member M2(_: A, _: int, a: Action<int>) = ()

let a = A()


a.M1(0, fun x -> ignore x)
a.M2(0, fun x -> ignore x)
