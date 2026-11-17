open System
open System.Runtime.CompilerServices

type A =
    static member G(x: int) = ()

[<Extension>]
type E =
    [<Extension>]
    static member Each(xs: int list, action: Action<int>) = ()

[1].Each(fun x -> A.G x)
E.Each([1], fun x -> A.G x)
