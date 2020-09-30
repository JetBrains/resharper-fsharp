module Module

type E =
    | Field1 = 1
    | [<CompiledName("Field2")>] Field2 = 2
