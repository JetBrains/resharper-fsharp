namespace global
type T() =
    [<CompiledName "A1">]
    member x.A
        with get (_: int) = 1
        and set (_: int) (_: int) = ()
