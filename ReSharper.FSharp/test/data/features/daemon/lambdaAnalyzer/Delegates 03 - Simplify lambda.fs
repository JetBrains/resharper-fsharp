open System

type A =
    static member M(_: Func<int, string, unit>) = ()

A.M(fun x y -> ignore y)
