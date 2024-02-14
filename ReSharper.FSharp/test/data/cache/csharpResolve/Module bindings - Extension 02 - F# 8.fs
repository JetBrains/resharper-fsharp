namespace Ns

open System.Runtime.CompilerServices

[<Extension>]
module Module1 =
    [<Extension; CompiledName("Ext")>]
    let ext (i: int) = i.ToString()

module Module2 =
    [<Extension; CompiledName("ExtNoTypeAttr")>]
    let ext (i: int) = i.ToString()
