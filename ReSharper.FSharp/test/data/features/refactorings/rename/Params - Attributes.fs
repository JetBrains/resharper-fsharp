module Module

type C() =
    member x.Foo([<CompiledName("Hello world")>] bar{caret}) =
        printfn "%s" bar
