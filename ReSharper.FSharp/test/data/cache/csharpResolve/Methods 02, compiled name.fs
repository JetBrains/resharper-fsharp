module Module

type T() =
    [<CompiledName("InstanceMethod")>]
    member x.Method() = 123

    [<CompiledName("StaticMethodCompiled")>]
    static member StaticMethod() = 123

    [<CompiledName("OverloadInt")>]
    member x.Overloads(a, b) = a + b

    [<CompiledName("OverloadString")>]
    member x.Overloads(a: string, b: string) = a + b

    [<CompiledName("StaticOverloadInt")>]
    static member StaticOverloads(a, b) = a + b

    [<CompiledName("StaticOverloadString")>]
    static member StaticOverloads(a: string, b: string) = a + b