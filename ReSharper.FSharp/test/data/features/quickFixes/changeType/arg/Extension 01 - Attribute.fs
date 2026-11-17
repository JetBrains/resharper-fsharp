module Module

open System.Runtime.CompilerServices

[<Extension>]
type E =
    [<Extension>]
    static member M(s: string, a: int, b: int) = ()

"".M(1, ""{caret})
