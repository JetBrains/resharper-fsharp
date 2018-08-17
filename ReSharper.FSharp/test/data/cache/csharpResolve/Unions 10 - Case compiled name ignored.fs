module Module

type U =
    | [<CompiledName("AName")>] CaseA
    | [<CompiledName("BName")>] CaseB of int
