module Module

type System.String with
    [<CompiledName("M1")>]
    member x.M1() = 123

    [<CompiledName("M2")>]
    member x.M2(i: int) = 123

    [<CompiledName("Prop")>]
    member x.Prop = 123
