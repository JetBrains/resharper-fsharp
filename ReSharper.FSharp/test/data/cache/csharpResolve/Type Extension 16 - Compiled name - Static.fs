module Module

type System.String with
    [<CompiledName("M1")>]
    static member M1() = 1

    [<CompiledName("M2")>]
    static member M2(x: int) = 1

    [<CompiledName("P")>]
    static member P = 1

let i1 = System.String.M1()
let i2 = System.String.M2(1)
let p = System.String.P
