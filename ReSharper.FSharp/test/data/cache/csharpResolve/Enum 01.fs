module Module

type E =
    | Field1 = 1
    | [<CompiledName("Field2Compiled")>] Field2 = 2
