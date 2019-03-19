module Module

type T() =
    member x.Method() = 123
    static member StaticMethod() = 123

    member x.Overloads(a, b) = a + b
    member x.Overloads(a: string, b: string) = a + b

    static member StaticOverloads(a, b) = a + b
    static member StaticOverloads(a: string, b: string) = a + b