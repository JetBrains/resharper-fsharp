namespace Ns

open System.Runtime.CompilerServices

[<Extension; Sealed; AbstractClass>]
type T() =
    [<Extension>]
    static member Ext(x: int) = x.ToString()
