namespace Ns

open System.Runtime.CompilerServices

[<Extension>]
module Module =
    [<Extension; CompiledName("Ext")>]
    let ext (x: int, y: int) = x.ToString()

