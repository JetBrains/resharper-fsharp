namespace global

open System.Runtime.CompilerServices

[<Extension>]
module Module =
    type System.Int32 with
        [<Extension; CompiledName("M")>]
        member x.M() = ()
