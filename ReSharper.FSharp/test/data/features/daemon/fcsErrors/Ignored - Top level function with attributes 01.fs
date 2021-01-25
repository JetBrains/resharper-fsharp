module Module

type A() =
    inherit System.Attribute()

type T() =
    [<A>]
    let f _ = ()
