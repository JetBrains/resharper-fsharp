[<CompiledName("TopCompiledName"); CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MyNamespace.Top

[<CompiledName("CompiledName")>]
module U =
    let x = 123

type U =
    | CaseA