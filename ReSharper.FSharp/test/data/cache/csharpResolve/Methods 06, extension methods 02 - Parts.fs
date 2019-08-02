namespace Ns

open System.Runtime.CompilerServices

[<AbstractClass; Sealed; Extension>]
type T() =
    class
    end

type T with
    [<Extension>]
    static member Ext(x: int) = x.ToString()
