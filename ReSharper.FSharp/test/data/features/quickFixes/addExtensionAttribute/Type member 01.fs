namespace Ns

open System.Runtime.CompilerServices

type T() =
    [<{caret}Extension>]
    static member M() = ()
