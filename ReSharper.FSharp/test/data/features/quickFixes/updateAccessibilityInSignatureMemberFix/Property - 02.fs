module A

type B() =
    member this.Foo{caret} with private get () = 1 and internal set (v: int) = ()
