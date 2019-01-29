namespace Ns

open System.Runtime.CompilerServices

[<Extension>]
module Module =
    [<Extension>]
    let ext (x: int) = x.ToString()
