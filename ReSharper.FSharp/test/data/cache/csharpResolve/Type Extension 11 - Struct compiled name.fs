module Module

[<Struct; CompiledName("T")>]
type S =
    struct
    end

type S with
    member x.Foo = 123
