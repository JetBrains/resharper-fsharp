module M

type A() = inherit System.Attribute()

[<AbstractClass>]
type T() =
    /// <summary>
    /// </summary>
    /// <param name="a"></param>
    member x.M(a, (x as y), [<A>] z, ?k) = a + x + y + z + k.Value

    /// <summary>
    /// </summary>
    /// <param name="x"></param>
    abstract member M1: x: int -> y: int -> int
