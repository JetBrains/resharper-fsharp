module Module

type T() =
    [<DefaultValue>] [<CompiledName("Compiled")>] val mutable Field: int
